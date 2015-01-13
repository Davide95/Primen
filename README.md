Primen
======
Primen is an educational software written in C# that implements prime factorization (using trial division)
with distributed computing (using MPI.NET)
and parallel computing (using System.Threading.Tasks).

For example Primen can factorize a product of two prime numbers in order to obtain an RSA private key from a public key.

How to use it
==================
Before using it, you have to install:
* [Windows HPC Server 2008](http://www.microsoft.com/en-us/download/details.aspx?id=6847).
* [MPI.NET Runtime](http://www.osl.iu.edu/research/mpi.net/files/1.0.0/MPI.NET%20Runtime.msi) in production or [MPI.NET SDK](http://www.osl.iu.edu/research/mpi.net/files/1.0.0/MPI.NET%20SDK.msi) for developing.

Then download the last Primen relase from https://github.com/Davide95/Primen/releases, unzip it and start on each machine 
```PowerShell
smpd -d
```
After this, go to one of the machines and run
```PowerShell
mpiexec -hosts N machine1 machine2 machineN C:\directory\when\it\was\unzipped\Primen.exe K
```
Where N is the number of hosts and K is the key to factorize.

For example, if you have 2 hosts called mOne and mTwo, the directory when you unzipped Primen is "C:\Primen" and you want to factorize 1000, you have to type
```PowerShell
mpiexec -hosts 2 mOne mTwo C:\Primen\Primen.exe 1000
```

How to test it
==================
Open a PowerShell window, go to the bin/Debug folder in Primen's project and run 
```PowerShell
mpiexec -n N Primen.exe K
```
Where N is the number of processes that you want to create and K is the key to factorize.

How to Engage, Contribute and Provide Feedback
==================
1. If you want to contribute, make sure that there is a corresponding issue for your change first. If there is none, create one.
2. Create a fork in GitHub.
3. Create a branch off the master branch with an adequate name.
4. Commit your changes and push your changes to GitHub.
5. Create a pull request against the origin's master branch.

###DOs and DON'Ts
* **DO** follow [C# Coding Conventions](http://msdn.microsoft.com/en-us/library/ff926074.aspx).
* **DO** run Code Analysis before committing your changes.

License
==================
This project is licensed under the [MIT license](LICENSE).

BigIntegerExtender is available as oper-source software under the [MIT license](BigintegerExtender LICENSE).

MPI.NET is available as open-source software under the [Boost Software License](MPI.NET LICENSE), which is a BSD-like license.
