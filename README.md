OPMStatusMonitor
================

This is a small console app that continuously monitors the government operating status as reported on OPM.gov. When the operating status changes, the app sets the color of a Philips Hue light to a corresponding color.

Configuration
=============

Everything is hard coded for now... fork it! (Or submit a pull request!!)

Run As a Service
================

This app is able to run as a service using the following command line argument:

```
OPMStatusMonitor.exe install
```

You must then start the service either though the MMC snapin, or by:

```
OPMStatusMonitor.exe start
```


To uninstall:

```
OPMStatusMonitor.exe uninstall
```

All topshelf command line arguments are supported:
http://docs.topshelf-project.com/en/latest/overview/commandline.html
