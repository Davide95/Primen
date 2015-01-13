$insertNumberOfProcessesMessage = "Number of processes: "
$insertKeyeMessage = "Key to factorize: "

[int]$numberOfProcesses = Read-Host $insertNumberOfProcessesMessage
[System.Numerics.BigInteger] $key = Read-Host $insertKeyeMessage

mpiexec -n "$numberOfProcesses" Primen.exe "$key"