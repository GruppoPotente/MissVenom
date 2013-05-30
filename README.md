MissVenom
=========

Standalone multifunctional proxy for WhatsApp in .NET

![MissVenom](https://dl.dropboxusercontent.com/u/68235039/venom.jpg)

Using modified WebServer library 2.0 from http://webserver.codeplex.com/

Using ARSoft.Tools.Net from http://arsofttoolsnet.codeplex.com/

Using WhatsApiNet library from http://github.com/shirioko/WhatsAPINet

Not compatible with Android 2.x (does not support installing root certificates)

###Root/jailbread/unlock not required###

Usage:
To sniff password:
- Make sure your mobile device is on the same WLAN subnet as your machine
- Unregister WhatsApp app on your device (e.g. wipe app data)
- Launch  MissVenom.exe and press Start
- Set DNS address on your device to the displayed IP address (in WiFi->Static IP configuration)
- Open your device's browser and go to https://cert.whatsapp.net to install root certificate
- Open and register WhatsApp on your device
- Your identity and password will appear in MissVenom as well as in MissVenom.log

To sniff TCP data (experimental):
- Make sure your mobile device is on the same WLAN subnet as your machine
- Launch MissVenom.exe
- Enter the device password, click the checkbox on the left and press Start
- Set DNS address on your device to the displayed IP address (in WiFi->Static IP configuration)
- Open WhatsApp
- Data will be logged to multiple files

TODO:
- Get proper key streams to decrypt all TCP messages (FML)
- Sniff audio stream hack on WP7
- Force client to re-authenticate with new challenge key to decrypt data (kind of working, needs optimization)
- ARP spoofing (making your life easier, one commit at a time)
