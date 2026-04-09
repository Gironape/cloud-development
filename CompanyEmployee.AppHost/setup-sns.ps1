Write-Host "Initializing LocalStack for SNS"

$maxAttempts = 30
$attempt = 0
while ($attempt -lt $maxAttempts) {
    try {
        awslocal sns list-topics 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "LocalStack is ready!"
            break
        }
    } catch {}
    $attempt++
    Write-Host "Waiting for LocalStack (attempt $attempt/$maxAttempts)"
    Start-Sleep -Seconds 1
}

if ($attempt -eq $maxAttempts) {
    Write-Host "ERROR: LocalStack failed to start"
    exit 1
}

Write-Host "Creating SNS topic..."
awslocal sns create-topic --name employee-events

Write-Host "Subscribing FileService to SNS topic..."
awslocal sns subscribe `
    --topic-arn arn:aws:sns:us-east-1:000000000000:employee-events `
    --protocol http `
    --notification-endpoint http://host.docker.internal:7277/api/sns/notification

Write-Host "SNS topic created and FileService subscribed successfully"