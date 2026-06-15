param(
    [string]$InputPath = (Join-Path $PSScriptRoot "chat.json"),
    [string]$OutputPath = (Join-Path $PSScriptRoot "chat.md"),
    [switch]$IncludeToolEvents
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

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

    $segments = New-Object System.Collections.Generic.List[string]

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
                    $segments.Add("> Tool: $toolMessage") | Out-Null
                }
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
                $segments.Add($text.Trim()) | Out-Null
                continue
            }
        }

        if ($item.PSObject.Properties.Name -contains "message") {
            $messageText = Get-MessageText -Message $item.message
            if (-not [string]::IsNullOrWhiteSpace($messageText)) {
                $segments.Add($messageText.Trim()) | Out-Null
            }
        }
    }

    return ($segments -join ([Environment]::NewLine + [Environment]::NewLine))
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

$lines.Add("# Copilot Chat Review Export") | Out-Null
$lines.Add("") | Out-Null
$lines.Add("Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss K")") | Out-Null
$lines.Add("Source: $InputPath") | Out-Null
$lines.Add("") | Out-Null
$detectiveEmoji = [char]::ConvertFromUtf32(0x1F575) + [char]0xFE0F + [char]0x200D + [char]0x2642 + [char]0xFE0F
$lines.Add("### $detectiveEmoji How to Review My AI Chat Logs Natively") | Out-Null
$lines.Add("If you want to view the full prompt engineering session exactly as it appeared in my workspace:") | Out-Null
$lines.Add("1. Open VS Code.") | Out-Null
$lines.Add('2. Open the Command Palette (`Ctrl+Shift+P` / `Cmd+Shift+P`).') | Out-Null
$lines.Add('3. Select **`Chat: Import Chat...`**') | Out-Null
$lines.Add('4. Select the file located at `docs/copilot-chat/chat.json` in this repository.') | Out-Null
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

$lines | Set-Content -LiteralPath $OutputPath -Encoding UTF8
Write-Host "Markdown export created: $OutputPath"