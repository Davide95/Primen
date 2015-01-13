param(
    [string]$file = 'Primen.exe',
    [int]$numberOfProcesses = (Read-Host 'Number of processes: '),
    [System.Numerics.BigInteger]$key = (Read-Host 'Key to factorize: ')
)

$Error211 = "Error 211: ""$file"" is not found."

if(-Not (Test-Path $file)) {
    write $ERROR_211
} else {
    mpiexec -n "$numberOfProcesses" "$file" "$key"
}