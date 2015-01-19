Param(
    [int]$numberOfVirtualNodes = (Read-Host 'Number of virtual nodes'),
    [string]$file = 'Primen.exe'
)

mpiexec -n $numberOfVirtualNodes "$file"