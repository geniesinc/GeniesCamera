# Crash Reporting

A simple package for easily adding different crash reporting services (Crashlytics, sentry etc..) without relying on a concrete implementation 
this allows to easily switch solutions without too many refactors.

## Components

### Genies.CrashReporting.CrashReporter

- Static class that registers different loggers to log incoming crash reports.

### Genies.CrashReporting.ICrashLogger

- Defines the crash logging contract.

## Usage

- For every new logger type, inherit `ICrashLogger` and implement it's methods
- Add the new crash logger instance to `CrashReporter` 
- Use `CrashReporter` throughout your codebase to log crashes/breadcrumbs
