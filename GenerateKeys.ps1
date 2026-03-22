# Generate RSA key pair for Epic SMART Backend Services
$rsa = [System.Security.Cryptography.RSA]::Create(2048)

# Export private key
$privateKey = $rsa.ExportRSAPrivateKeyPem()
$privateKey | Out-File -FilePath "epic_private_key.pem" -Encoding utf8

# Export public key
$publicKey = $rsa.ExportSubjectPublicKeyInfoPem()
$publicKey | Out-File -FilePath "epic_public_key.pem" -Encoding utf8

Write-Host "Keys generated successfully!"
Write-Host "Private key: epic_private_key.pem"
Write-Host "Public key:  epic_public_key.pem"