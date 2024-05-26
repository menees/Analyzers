// Enable parallelization of unit tests at the method level using an assembly-level attribute.
// This way it's picked up by the unit test runner even if no .runsettings file is used or if
// an alternate .runsettings file is used.
// https://devblogs.microsoft.com/devops/mstest-v2-in-assembly-parallel-test-execution/
[assembly: Parallelize(Scope = Microsoft.VisualStudio.TestTools.UnitTesting.ExecutionScope.MethodLevel, Workers = 8)]
