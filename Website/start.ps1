# FrogRunner local server (PowerShell, no Python needed)
$port = 8080
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://+:$port/")
$listener.Start()
Write-Host "Serving FrogRunner site at http://localhost:$port/"
Write-Host "Press Ctrl+C to stop."
Start-Process "http://localhost:$port/"
while ($listener.IsListening) {
    $ctx = $listener.GetContext()
    $req = $ctx.Request
    $res = $ctx.Response
    $path = $req.Url.LocalPath
    if ($path -eq "/") { $path = "/index.html" }
    $file = Join-Path $root $path.TrimStart("/")
    if (Test-Path $file -PathType Leaf) {
        $bytes = [System.IO.File]::ReadAllBytes($file)
        $ext = [System.IO.Path]::GetExtension($file).ToLower()
        $mime = switch ($ext) {
            ".html" { "text/html; charset=utf-8" }
            ".css"  { "text/css" }
            ".js"   { "application/javascript" }
            ".apk"  { "application/vnd.android.package-archive" }
            ".zip"  { "application/zip" }
            ".txt"  { "text/plain" }
            ".md"   { "text/markdown" }
            default { "application/octet-stream" }
        }
        $res.ContentType = $mime
        $res.OutputStream.Write($bytes, 0, $bytes.Length)
    } else {
        $res.StatusCode = 404
        $msg = [System.Text.Encoding]::UTF8.GetBytes("404 Not Found")
        $res.OutputStream.Write($msg, 0, $msg.Length)
    }
    $res.Close()
}