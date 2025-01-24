# Clustered-Timer-App (Clustered Timer Application)

## Overview
The Clustered Timer Application leverages a distributed lock to ensure that, in a clustered environment, only one instance of the timer acquires the lock and executes the job. The timer's state is stored in a database, with current support for MySQL and MongoDB.

## Features
- Distributed locking mechanism
- Support for MySQL and MongoDB
- Configurable job execution intervals
- Logging of application events

## Getting Started

### Prerequisites
- .NET SDK
- MongoDB or MySQL server

### Installation
1. Clone the repository:
2. Navigate to the project directory:
3. Restore the dependencies:
### Configuration
- Update the `databaseType` and `connectionString` variables in `Program.cs` to match your database setup.

### Running the Application
To run the application, use the following command:
1. Set the database type and connection string in the Program.cs file.
2. Build the project and run.

In order to run multipe instances and test:
1. Set the $executablePath variable & $instanceCount in the StartMultipleInstances.ps1 file.
2. Run the StartMultipleInstances.ps1 file.			
3. A new terminal window will open for each instance.

## License
This project is licensed under the MIT License.
