# DirectoryPounder

This program is a simple command line utility designed to "pound" on a directory
by repeatedly creating random files,
directories, and files within those subdirectories. It randomly reads the files
(with a focus on recently created ones, including an immediate read of each file written).
It also randomly deletes both files and directories that it has written.

## Usage

At a command line, CD to the directory that should be pounded.
Generally, it should be empty to begin with; this allows Pounder to accurately know
what files exist. Also, while it is unlikely, Pounder could randomly generate a
filename that matches an existing file and overwrite it. Pounder will only delete
files it has created. Typically, there will be some left over at the end that you
will need to delete manually; this is also easier if there is nothing you want
in the folder.

The command line argument -r may be used to have Pounder use our RobustFile code.
Otherwise it uses standard File and Directory methods.

Press any key to quit and see a report of any errors.

# License

DirectoryPounder is open source, using the [MIT License](http://sil.mit-license.org). It is Copyright SIL International.