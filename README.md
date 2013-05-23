MissVenom
=========

Standalone multifunctional proxy for WhatsApp in .NET

![MissVenom](https://dl.dropboxusercontent.com/u/68235039/venom.jpg)

Using WebServer library 2.0 from http://webserver.codeplex.com/

Using ARSoft.Tools.Net from http://arsofttoolsnet.codeplex.com/

Not compatible with Android 2.x (does not support installing root certificates)

###Root/jailbread/unlock not required###

Usage:
- Make sure your mobile device is on the same subnet as your machine
- Install server.crt as trusted root certificate on your mobile device
- Unregister WhatsApp app (e.g. wipe app data)
- Start MissVenom.exe
- Set DNS address on your phone to the displayed IP address (in WiFi->Static IP configuration)
- **Reboot phone to clear DNS cache before proceeding**
- Register WhatsApp on your device
- Your identity and password will appear in MissVenom

TODO:
- TCP dump (currently working on)
- TCP decryption (currently working on)
- ContactSync intercept
- Logging
- Deserialize GET and JSON for nice formatting
- ARP spoofing (making your life easier, one commit at a time)
