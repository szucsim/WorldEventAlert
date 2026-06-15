param(
    [string]$InputPath = (Join-Path $PSScriptRoot "chat.json"),
    [string]$OutputPath = (Join-Path $PSScriptRoot "chat.md"),
    [switch]$IncludeToolEvents,
    [ValidateSet("EmailAttachment", "RepositoryPath")]
    [string]$ImportSourceMode = "EmailAttachment",
    [string]$RepositoryJsonPath = "docs/copilot-chat/chat.json",
    [string]$AttachmentFileName = "chat.json",
    [bool]$RedactSensitiveContent = $true,
    [bool]$StripEmptyCodeBlocks = $true
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Sanitize-Text {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text,
        [Parameter(Mandatory = $true)]
        [bool]$EnableRedaction
    )

    if (-not $EnableRedaction -or [string]::IsNullOrEmpty($Text)) {
        return $Text
    }

    $sanitized = $Text

    # Redact Slack incoming webhooks to avoid accidental secret-scanning blocks.
    $sanitized = [Regex]::Replace(
        $sanitized,
        'https://hooks\.slack\.com/services/[A-Za-z0-9/_\-]+'
        ,
        'https://hooks.slack.com/services/REDACTED'
    )

    # Redact common token patterns that may appear in exported logs.
    $sanitized = [Regex]::Replace($sanitized, '\bghp_[A-Za-z0-9]{20,}\b', 'ghp_REDACTED')
    $sanitized = [Regex]::Replace($sanitized, '\bxox[baprs]-[A-Za-z0-9\-]{10,}\b', 'xox_REDACTED')

    return $sanitized
}

function Normalize-TranscriptText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Text,
        [Parameter(Mandatory = $true)]
        [bool]$RemoveEmptyCodeBlocks
    )

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $Text
    }

    $normalized = $Text

    if ($RemoveEmptyCodeBlocks) {
        # Remove empty fenced code blocks that render as large blank panels in markdown viewers.
        $normalized = [Regex]::Replace(
            $normalized,
            '(?ms)^\s*```\s*\r?\n(?:\s*\r?\n)*\s*```\s*(\r?\n)?',
            ''
        )
    }

    # Keep transcript readable by limiting long blank runs.
    $normalized = [Regex]::Replace($normalized, '(\r?\n){3,}', [Environment]::NewLine + [Environment]::NewLine)

    return $normalized.Trim()
}

function Get-MessageText {
    param(
        [Parameter(Mandatory = $false)]
        [object]$Message
    )

    if ($null -eq $Message) {
        return ""
    }

    if ($Message.PSObject.Properties.Name -contains "text" -and -not [string]::IsNullOrWhiteSpace([string]$Message.text)) {
        return [string]$Message.text
    }

    if ($Message.PSObject.Properties.Name -contains "parts" -and $null -ne $Message.parts) {
        $parts = New-Object System.Collections.Generic.List[string]

        foreach ($part in $Message.parts) {
            if ($null -ne $part -and $part.PSObject.Properties.Name -contains "text" -and -not [string]::IsNullOrWhiteSpace([string]$part.text)) {
                $parts.Add([string]$part.text) | Out-Null
            }
        }

        return ($parts -join [Environment]::NewLine)
    }

    return ""
}

function Get-ToolMessage {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Item
    )

    if ($Item.PSObject.Properties.Name -contains "invocationMessage") {
        $invocationMessage = $Item.invocationMessage
        if ($invocationMessage -is [string]) {
            return $invocationMessage
        }

        if ($null -ne $invocationMessage -and $invocationMessage.PSObject.Properties.Name -contains "value") {
            return [string]$invocationMessage.value
        }
    }

    if ($Item.PSObject.Properties.Name -contains "pastTenseMessage") {
        $pastTenseMessage = $Item.pastTenseMessage
        if ($pastTenseMessage -is [string]) {
            return $pastTenseMessage
        }

        if ($null -ne $pastTenseMessage -and $pastTenseMessage.PSObject.Properties.Name -contains "value") {
            return [string]$pastTenseMessage.value
        }
    }

    return ""
}

function Get-AssistantText {
    param(
        [Parameter(Mandatory = $false)]
        [object[]]$Response,
        [Parameter(Mandatory = $true)]
        [bool]$IncludeToolLogs
    )

    if ($null -eq $Response) {
        return ""
    }

    $builder = New-Object System.Text.StringBuilder

    function Append-ChunkText {
        param(
            [Parameter(Mandatory = $true)]
            [System.Text.StringBuilder]$Target,
            [Parameter(Mandatory = $true)]
            [string]$Chunk
        )

        if ([string]::IsNullOrWhiteSpace($Chunk)) {
            return
        }

        if ($Target.Length -gt 0) {
            $lastChar = $Target[$Target.Length - 1]
            $firstChar = $Chunk[0]
            if (-not [char]::IsWhiteSpace($lastChar) -and -not [char]::IsWhiteSpace($firstChar)) {
                [void]$Target.Append([Environment]::NewLine + [Environment]::NewLine)
            }
        }

        [void]$Target.Append($Chunk)
    }

    foreach ($item in $Response) {
        if ($null -eq $item) {
            continue
        }

        $kind = ""
        if ($item.PSObject.Properties.Name -contains "kind") {
            $kind = [string]$item.kind
        }

        if ($kind -eq "thinking") {
            continue
        }

        if ($kind -eq "toolInvocationSerialized") {
            if ($IncludeToolLogs) {
                $toolMessage = Get-ToolMessage -Item $item
                if (-not [string]::IsNullOrWhiteSpace($toolMessage)) {
                    Append-ChunkText -Target $builder -Chunk "> Tool: $toolMessage"
                }
            }

            continue
        }

        if ($kind -eq "inlineReference") {
            $referenceName = ""
            if ($item.PSObject.Properties.Name -contains "name" -and -not [string]::IsNullOrWhiteSpace([string]$item.name)) {
                $referenceName = [string]$item.name
            }
            elseif ($item.PSObject.Properties.Name -contains "inlineReference" -and $null -ne $item.inlineReference -and $item.inlineReference.PSObject.Properties.Name -contains "path") {
                $referenceName = [string]$item.inlineReference.path
                $referenceName = $referenceName -replace '^/[A-Za-z]:/', ''
            }

            if (-not [string]::IsNullOrWhiteSpace($referenceName)) {
                [void]$builder.Append("[$referenceName]($referenceName)")
            }

            continue
        }

        if ($item.PSObject.Properties.Name -contains "value") {
            $value = $item.value
            $text = ""

            if ($value -is [string]) {
                $text = $value
            }
            elseif ($null -ne $value -and $value.PSObject.Properties.Name -contains "value") {
                $text = [string]$value.value
            }

            if (-not [string]::IsNullOrWhiteSpace($text)) {
                Append-ChunkText -Target $builder -Chunk $text
                continue
            }
        }

        if ($item.PSObject.Properties.Name -contains "message") {
            $messageText = Get-MessageText -Message $item.message
            if (-not [string]::IsNullOrWhiteSpace($messageText)) {
                Append-ChunkText -Target $builder -Chunk $messageText
            }
        }
    }

    return $builder.ToString().Trim()
}

if (-not (Test-Path -LiteralPath $InputPath -PathType Leaf)) {
    throw "Input file not found: $InputPath"
}

$rawJson = Get-Content -LiteralPath $InputPath -Raw -Encoding UTF8
$chat = $rawJson | ConvertFrom-Json

if ($null -eq $chat -or $null -eq $chat.requests) {
    throw "Invalid chat export format. Expected top-level 'requests' array."
}

$lines = New-Object System.Collections.Generic.List[string]

$sourceLine = if ($ImportSourceMode -eq "EmailAttachment") {
    "Source: Attached file ($AttachmentFileName via email)"
}
else {
    "Source: $RepositoryJsonPath"
}

$step4Instruction = if ($ImportSourceMode -eq "EmailAttachment") {
    '4. Select the attached `' + $AttachmentFileName + '` file from the email.'
}
else {
    '4. Select the file located at `' + $RepositoryJsonPath + '` in this repository.'
}

$lines.Add("# Copilot Chat Review Export") | Out-Null
$lines.Add("") | Out-Null
$lines.Add("Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss K")") | Out-Null
$lines.Add($sourceLine) | Out-Null
$lines.Add("") | Out-Null
$detectiveEmoji = [char]::ConvertFromUtf32(0x1F575) + [char]0xFE0F + [char]0x200D + [char]0x2642 + [char]0xFE0F
$lines.Add("### $detectiveEmoji How to Review My AI Chat Logs Natively") | Out-Null
$lines.Add("If you want to view the full prompt engineering session exactly as it appeared in my workspace:") | Out-Null
$lines.Add("1. Open VS Code.") | Out-Null
$lines.Add('2. Open the Command Palette (`Ctrl+Shift+P` / `Cmd+Shift+P`).') | Out-Null
$lines.Add('3. Select **`Chat: Import Chat...`**') | Out-Null
$lines.Add($step4Instruction) | Out-Null
$lines.Add("") | Out-Null
$lines.Add("## Conversation") | Out-Null

$turn = 0
foreach ($request in $chat.requests) {
    $turn++

    $userText = Get-MessageText -Message $request.message
    $assistantText = Get-AssistantText -Response $request.response -IncludeToolLogs ([bool]$IncludeToolEvents)

    if ([string]::IsNullOrWhiteSpace($userText)) {
        $userText = "_No user text captured in this turn._"
    }

    if ([string]::IsNullOrWhiteSpace($assistantText)) {
        $assistantText = "_No assistant text captured in this turn._"
    }

    $userText = Sanitize-Text -Text $userText -EnableRedaction $RedactSensitiveContent
    $assistantText = Sanitize-Text -Text $assistantText -EnableRedaction $RedactSensitiveContent
    $userText = Normalize-TranscriptText -Text $userText -RemoveEmptyCodeBlocks $StripEmptyCodeBlocks
    $assistantText = Normalize-TranscriptText -Text $assistantText -RemoveEmptyCodeBlocks $StripEmptyCodeBlocks

    $lines.Add("") | Out-Null
    $lines.Add("### Turn $turn") | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add("#### User") | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add($userText.Trim()) | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add("#### Assistant") | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add($assistantText.Trim()) | Out-Null
}

$outputDirectory = Split-Path -Path $OutputPath -Parent
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path -LiteralPath $outputDirectory -PathType Container)) {
    New-Item -Path $outputDirectory -ItemType Directory -Force | Out-Null
}

$filteredLines = New-Object System.Collections.Generic.List[string]

for ($index = 0; $index -lt $lines.Count; $index++) {
    $current = $lines[$index]

    if ($current.Trim() -eq '```') {
        $scan = $index + 1
        while ($scan -lt $lines.Count -and [string]::IsNullOrWhiteSpace($lines[$scan])) {
            $scan++
        }

        if ($scan -lt $lines.Count -and $lines[$scan].Trim() -eq '```') {
            $index = $scan

            while (($index + 1) -lt $lines.Count -and [string]::IsNullOrWhiteSpace($lines[$index + 1])) {
                $index++
            }

            continue
        }
    }

    $filteredLines.Add($current) | Out-Null
}

$finalLines = New-Object System.Collections.Generic.List[string]
$previousBlank = $false

foreach ($line in $filteredLines) {
    $trimmed = $line.Trim()
    if ($trimmed -eq '-' -or $trimmed -eq '*') {
        continue
    }

    $isBlank = [string]::IsNullOrWhiteSpace($line)
    if ($isBlank -and $previousBlank) {
        continue
    }

    $finalLines.Add($line) | Out-Null
    $previousBlank = $isBlank
}

$finalLines | Set-Content -LiteralPath $OutputPath -Encoding UTF8
Write-Host "Markdown export created: $OutputPath"