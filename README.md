# Shadow

A simple one-way directory synchronization tool that let's you create a ***shadow*** of a directory.

## Usage
  `Shadow [options]`

### Options
| Option                                     |       Description                     |
|--------------------------------------------|---------------------------------------|
| -s, --source <source> (REQUIRED)           | Source directory.                     |
| -d, --destination <destination> (REQUIRED) | Destination directory.                |
| -i, --interval <interval> (REQUIRED)       | Synchronization interval in seconds.  |
| -l, --log <log>                            | Log file path.                        |
| --once                                     | Sync only once.                       |
| -?, -h, --help                             |Show help and usage information.       |
| --version                                  | Show version information.             |
