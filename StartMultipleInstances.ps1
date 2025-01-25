# Define the path to the executable
$executablePath = ".\bin\Debug\net8.0\ClusteredTimerApp.exe" # Replace with the path to your application executable

# Set the number of instances
$instanceCount = 3 # Replace with the desired number of instances

# Loop to start the instances
for ($i = 1; $i -le $instanceCount; $i++) {
    # Start each instance in a new console window
    Start-Process -FilePath $executablePath -ArgumentList "--instanceId $i"
    
    Write-Host "Started instance $i in a separate console."
}

Write-Host "All instances started."

