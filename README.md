MissVenom
=========

Small standalone SSL proxy for WhatsApp in .NET based on my upcoming MissProxy.

![MissVenom](https://dl.dropboxusercontent.com/u/68235039/proxy.png)

Using WebServer library 2.0 from http://webserver.codeplex.com/
Using ARSoft.Tools.Net from http://arsofttoolsnet.codeplex.com/

Not compatible with Android 2.x (does not support installing root certificates)

###Root/jailbread/unlock not required###

Usage:
- Make sure your mobile device is on the same subnet as your machine
- Install server.crt as trusted root certificate on your mobile device
- Unregister WhatsApp app (e.g. wipe app data)
- Set DNS address on your phone to your computer's IP address (in WiFi->Static IP configuration)
- **Reboot phone to clear DNS cache before proceeding**
- Start MissVenom.exe
- Register WhatsApp on your device
- Your identity and password will appear in MissVenom

TODO:
- Logging
- Deserialize GET and JSON for nice formatting
- ARP spoofing (making your life easier, one commit at a time)
