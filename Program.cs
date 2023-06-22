// Create random files and directories in the current directory. Log failures.
var pounder = new DirectoryPounder.DirectoryPounder(Directory.GetCurrentDirectory());
pounder.UseRobust = args.Length > 0 && args[0] == "-r";
pounder.PoundOnDirectory();
