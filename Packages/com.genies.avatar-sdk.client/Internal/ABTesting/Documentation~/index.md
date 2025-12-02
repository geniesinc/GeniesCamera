# A/B Testing Service

This package provides an interface implementation of an A/B testing service. Can be used to check feature gates, remote configurations, etc...

# How to use

Besides defining the interface, this package doesn't have a default implementation so its on the user of the package to implement.

Just extend `IABTestingService` and create your own implementation with any service you want to use (Statsig, in-house, etc...)