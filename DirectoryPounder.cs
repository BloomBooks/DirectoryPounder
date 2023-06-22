using System.Diagnostics;
using System.Text;
using SIL.Extensions;
using SIL.IO;

namespace DirectoryPounder
{

	/// <summary>
	/// This class is designed to "pound" on a directory by repeatedly creating random files,
	/// directories, and files within those subdirectories. It randomly reads the files
	/// (with a focus on recently created ones, including an immediate read of each file written).
	/// It also randomly deletes both files and directories.
	/// To use: at a command line, CD to the directory that should be pounded.
	/// Generally, it should be empty to begin with; this allows Pounder to accurately know
	/// what files exist. Also, while it is unlikely, Pounder could randomly generate a
	/// filename that matches an existing file and overwrite it. Pounder will only delete
	/// files it has created. Typically, there will be some left over at the end that you
	/// will need to delete manually.
	/// The command line argument -r may be used to have Pounder use our RobustFile code.
	/// Otherwise it uses standard File and Directory methods.
	/// Press any key to quit and see a report of any errors.
	/// </summary>
	public class DirectoryPounder
	{
		public bool UseRobust;
		private readonly Dictionary<string, string> _files = new();
		private readonly Random _random = new();
		private string _path;
		private string _rootPath;
		private readonly List<string> _recentFiles = new();
		private int _written = 0;
		private int _deleted = 0;
		private int _read = 0;
		private int _overwritten = 0;
		private int _directoriesMade = 0;
		private int _directoriesDeleted = 0;
		private readonly List<string> _directories = new();
		private readonly List<string> _errors = new();

		public DirectoryPounder(string path)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			if (!Directory.Exists(path))
				throw new ArgumentException("Directory to pound not found: " + nameof(path));
			_path = path;
			_rootPath = _path;
		}

		public void PoundOnDirectory()
		{
			int count = 0;
			while (!Console.KeyAvailable)
			{
				if (count++ >= 100)
				{
					Log($"Written {_written}, deleted {_deleted}, read {_read}, overwritten {_overwritten}, made {_directoriesMade} directories and deleted {_directoriesDeleted}, {_errors.Count} errors", true);
					count = 0;
				}

				try
				{
					var which = _random.Next(9);
					switch (which)
					{
						case 0:
							MakeADirectory();
							break;
						case 1:
							WriteARandomFile();
							break;
						case 2:
							ReadARandomFile();
							break;
						case 3:
							DeleteARandomFile();
							break;
						case 4:
							ReadARecentFile();
							break;
						case 5:
							WriteAndDeleteAFile();
							break;
						case 6:
							OverwriteARandomFile();
							break;
						case 7:
							PopDirectory();
							break;
						case 8:
							DeleteADirectory();
							break;
					}
				}
				catch (Exception ex)
				{
					Log(ex.Message);
				}
			}

			Log("", true);
			if (_errors.Count > 0)
			{
				Log("Errors reported:", true);
				foreach (var error in _errors)
					Log(error, true);
			}
			else
			{
				Log("No problems detected", true);
			}
		}

		private void MakeADirectory()
		{
			var name = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
			var newPath = Path.Combine(_path, name);
			try
			{
				CreateDirectory(newPath);
				_directories.Add(newPath);
				// start making stuff in it
				_path = newPath;
			}
			catch (Exception ex)
			{
				Log($"MakeDirectory failed at {newPath} with {ex.Message}");
			}
		}

		private void PopDirectory()
		{
			if (_path == _rootPath)
				return;
			// Randomly, stay in the new directory a bit longer so more stuff gets
			// created there.
			if (_random.Next(10) != 0)
				return;
			_path = Path.GetDirectoryName(_path)!;
		}

		private void DeleteADirectory()
		{
			if (_directories.Count == 0) return;
			// Wiping out a directory undoes a lot of file (and possibly directory) creation.
			// We want it to happen a bit less often than other changes so the directory fills
			// up a bit.
			if (_random.Next(10) > 3)
				return;
			var dir = _directories[_random.Next(_directories.Count)];
			try
			{
				DeleteDirectory(dir);
				_directories.RemoveAll(x => x.StartsWith(dir));
				_recentFiles.RemoveAll(x => x.StartsWith(dir));
				if (_path.StartsWith(dir))
					_path = _rootPath;
				_files.RemoveAll(kvp => kvp.Key.StartsWith(dir));
			}
			catch (Exception ex)
			{
				Log($"DeleteDirectory failed to delete {dir} with {ex.Message}");
			}
		}


		private void OverwriteARandomFile()
		{
			var path = PickARandomFile();
			if (path == null)
				return;
			WriteAFileWithRandomContent(path);
		}

		private void WriteAndDeleteAFile()
		{
			var path = WriteARandomFile();
			if (path != null)
				Delete(path);
		}

		private void DeleteARandomFile()
		{
			if (_random.Next(10) > 7) // Let them accumulate a bit
				return;
			var path = PickARandomFile();
			if (path == null)
				return;
			Delete(path);
		}

		private void Delete(string path)
		{
			try
			{
				DeleteFile(path);
			_files.Remove(path);
			_recentFiles.RemoveAll(x => x == path); // may have written repeatedly and recently.
			}
			catch (Exception ex)
			{
				Log($"Deleting {path} failed: {ex.Message}");
			}
		}

		private void ReadARandomFile()
		{
			var path = PickARandomFile();
			if (path == null)
				return;
			ReadFile(path);
		}

		private void ReadARecentFile()
		{
			if (_recentFiles.Count == 0)
				return;
			var path = _recentFiles[_random.Next(_recentFiles.Count)];
			ReadFile(path);
		}

		private string? PickARandomFile()
		{
			string[] keys = _files.Keys.ToArray();
			if (keys.Length == 0)
				return null;
			var path = keys[_random.Next(keys.Length)];
			return path;
		}

		private void ReadFile(string path)
		{
			try
			{
				var content = ReadAllText(path);
				if (content != _files[path])
					Log($"Wrong content read from {path}\n" +
					    $"should have been\n"
					    + _files[path]
					    + "\nbut was\n"
					    + content);
			}
			catch (Exception ex)

			{
				Log($"ReadFile failed on {path} with exception {ex.Message}");
			}
		}

		private string? WriteARandomFile()
		{
			var path = Path.Combine(_path, Path.GetRandomFileName());
			return WriteAFileWithRandomContent(path);
		}

		private string? WriteAFileWithRandomContent(string path)
		{
			var baseText = "This is some random stuff ";
			var builder = new StringBuilder();
			var howMuch = _random.Next(300);
			for (int i = 0; i < howMuch; i++)
			{
				builder.Append(baseText);
				builder.Append(_random.Next(1000));
				builder.Append(Environment.NewLine);
			}

			var content = builder.ToString();

			// Do NOT want to use File.Exists here, because that's a file operation.
			// By design program should be given an initially empty directory.
			var exists = _files.ContainsKey(path);
			_files[path] = content;
			_recentFiles.Add(path);
			if (_recentFiles.Count > 9)
			{
				_recentFiles.RemoveAt(0);
			}

			try
			{
				WriteAllText(path, content, exists);
			}
			catch (Exception ex)
			{
				var status = exists ? "was being overwritten" : "was new";
				Log($"WriteFile failed on file {path}, which {status}. {ex.Message}");
				return null;
			}

			// Make sure we can read it back immediately
			ReadFile(path);
			return path;
		}

		void Log(string message, bool info = false)
		{
			Console.WriteLine(message);
			Debug.WriteLine(message);
			if (info == false)
				_errors.Add(message);
		}

		void WriteAllText(string path, string content, bool exists)
		{
			_written++;
			//Log("   Writing " + path);
			if (exists)
			{
				_overwritten++;

			}
			if (UseRobust)
			{
				RobustFile.WriteAllText(path, content);
			}
			else
			{
				File.WriteAllText(path, content);
			}
		}
		string ReadAllText(string path)
		{
			_read++;
			if (UseRobust)
			{
				return RobustFile.ReadAllText(path);
			}
			else
			{
				return File.ReadAllText(path);
			}
		}

		private void DeleteFile(string path)
		{
			_deleted++;
			//Log("   Deleting " + path);
			if (UseRobust)
			{
				RobustFile.Delete(path);
			}
			else
			{
				File.Delete(path);
			}
		}

		private void CreateDirectory(string path)
		{
			_directoriesMade++;
			// We don't have a more robust way to do this yet
			Directory.CreateDirectory(path);
		}

		private void DeleteDirectory(string path)
		{
			_directoriesDeleted++;
			//Log("   Deleting " + path);
			if (UseRobust)
			{
				RobustIO.DeleteDirectoryAndContents(path);
			}
			else
			{
				Directory.Delete(path, true);
			}
		}
	}
}
