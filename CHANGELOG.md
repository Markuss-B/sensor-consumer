# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- Added new fields to Sensor: latestMeasurements and lastMeasurementTimestamp.
- Added TopicSchema to allow more flexible topic handling.

## [2.0.0]
- Release version 2.0.0 along side first releases to production for webapi, dashboard, courier.
### Added
- Topic saving. Adds topics to sensor topic set.
- Inactive sensor caching to avoid processing inactive sensors.
- Sensor metadata time series updates.
### Deprecated
- Individual metadata saving from topic. Still functional but not to be used in prod until further notice.

## [1.0.0]
### Removed
- Raw data saving to the database. Now only saves measurements with time converted from unix timestamp to datetime.
- This will be the first deployed version.

## [1.0.0-alpha.1] - 2024-11-05
### Added
- Initial setup for the experimental version.
- Currently listens to mqtt consumer and saves it to the database.
- Basic topic schema logic.
- Saves both raw measurements and measurements with time converted from unix timestamp to datetime.