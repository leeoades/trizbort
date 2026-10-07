# Manual test maps

This directory contains `.trizbort` fixtures and batch/script runners for manual and visual
regression checks. These files are not discovered by `dotnet test`; automated tests live in
`src/Trizbort.Tests/`.

The batch runners resolve their fixture paths from the runner's own directory, so they can be
started from another working directory. Opening the map files still requires Trizbort to be
installed or associated with the `.trizbort` extension.
