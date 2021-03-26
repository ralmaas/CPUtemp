# CPUtemp
Replacement for CPUtemperature

A small Windows Service program used for reporting the CPU temperature using MQTT at regular intervals.

One NuGet packages used: M2Mqtt(4.3.0.0).
In addition the OpenHardwareMonitorLib.dll have been added (version 0.9.5) since the NuGet package was very old (0.7.1.0).

## Testing
This code <b>has</b> been verified on both Intel and AMD CPU.

roar@nsi:~$ mosquitto_sub -h localhost -t Asus/# -v
Laptop/Temp/CPU 51
Laptop/Temp/CPU 53
