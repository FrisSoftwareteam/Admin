Add-Type -AssemblyName System.Drawing
$imagePath = "c:\Users\Administrator\Documents\My Projects\FirstReg-master-main (5)\FirstReg-master-main\Web\wwwroot\img\slide-2.jpg"
$image = [System.Drawing.Image]::FromFile($imagePath)
Write-Output "Width: $($image.Width)"
Write-Output "Height: $($image.Height)"
$image.Dispose()
