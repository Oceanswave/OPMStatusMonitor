OPMStatusMonitor
================

This is a small console app that continuously monitors the government operating status as reported on OPM.gov. When the operating status changes, the app sets the color of a Philips Hue light to a corresponding color.

Configuration
=============

Everything is hard coded for now... fork it! (Or submit a pull request!!)

Run As a Service
================

This app is able to run as a service using the following command line argument:

OPMStatusMonitor.exe install

To uninstall:

OPMStatusMonitor.exe uninstall
