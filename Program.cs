using System.Diagnostics;
using System.Reflection;

string dirToSearch;

if (args.Length == 0)
{
    dirToSearch = Directory.GetCurrentDirectory();
    Console.WriteLine($"No argument supplied, will search current working directory: {dirToSearch}.");
}
else
{
    dirToSearch = args[0];
}

var pdbOnlyReleaseBuilds = new List<string>();
var debuggableReleaseBuilds = new List<string>();
var partialDebugUnoptimisedBuilds = new List<string>();
var fullDebugBuilds = new List<string>();
var fullReleaseBuilds = new List<string>();

foreach (var assembly in Directory.EnumerateFiles(dirToSearch, "*.dll", SearchOption.AllDirectories))
{
    try
    {
        var attribs = Assembly.LoadFile(assembly).GetCustomAttributes(typeof(DebuggableAttribute), false);

        // If the 'DebuggableAttribute' is not found then it is definitely an OPTIMIZED build
        if (attribs.Length > 0)
        {
            // Just because the 'DebuggableAttribute' is found doesn't necessarily mean
            // it's a DEBUG build; we have to check the JIT Optimization flag
            // i.e. it could have the "generate PDB" checked but have JIT Optimization enabled
            if (attribs[0] is DebuggableAttribute debuggableAttribute)
            {
                var buildType = debuggableAttribute.IsJITOptimizerDisabled ? "Debug" : "Release";

                // check for Debug Output "full" or "pdb-only"
                var debugOutput = (debuggableAttribute.DebuggingFlags &
                                   DebuggableAttribute.DebuggingModes.Default) !=
                                  DebuggableAttribute.DebuggingModes.None
                    ? "Full"
                    : "pdb-only";

                if (buildType == "Release")
                {
                    if (debugOutput == "Full")
                    {
                        debuggableReleaseBuilds.Add(assembly);
                    }
                    else
                    {
                        pdbOnlyReleaseBuilds.Add(assembly);
                    }
                }
                else
                {
                    if (debugOutput == "Full")
                    {
                        fullDebugBuilds.Add(assembly);
                    }
                    else
                    {
                        partialDebugUnoptimisedBuilds.Add(assembly);
                    }
                }
            }
        }
        else
        {
            fullReleaseBuilds.Add(assembly);
        }
    }
    catch
    {
        // ignored
    }
}

void OutputBuildsFound(List<string> assemblies, string type)
{
    Console.WriteLine($"Found {assemblies.Count} assemblies of type {type}");
    foreach (var assembly in assemblies)
    {
        Console.WriteLine($"\t{assembly}");
    }
}


OutputBuildsFound(pdbOnlyReleaseBuilds, nameof(pdbOnlyReleaseBuilds));
OutputBuildsFound(debuggableReleaseBuilds, nameof(debuggableReleaseBuilds));
OutputBuildsFound(partialDebugUnoptimisedBuilds, nameof(partialDebugUnoptimisedBuilds));
OutputBuildsFound(fullReleaseBuilds, nameof(fullReleaseBuilds));
OutputBuildsFound(fullDebugBuilds, nameof(fullDebugBuilds));