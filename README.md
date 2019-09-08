# ZipStudio
A tool for converting Koikatsu mods to sideloader.

Uses parts of AssetStudio  
Copyright (c) 2016-2018 Perfare  
https://github.com/Perfare/AssetStudio

This tool is for converting hard-mods to sideloader mods, complete with automatic list conversion to .csv
It tries to be smart about what to copy but may not be perfect

The general workflow is:
1. Extract the mod (maybe to a temp folder)
2. Open this tool, and click Tools -> Import from... -> Folder
3. Select the new folder you extracted
4. It has been converted now, but you still need to update the manifest data, so open the Manifest editor tab
5. After filling it to your heart's content (note below about GUID), click File -> Save


Note about GUID:

Since the auto-resolver relies on the GUID being set by the modder to a constant value to work, you're going to run into problems if two people convert a mod but use different GUIDs. Check with whoever is maintaining the sideloader modpacks before converting anyone else's mods. For Koikatsu and EmotionCreators contact Anon11. For AI Girl contact ScrewThisNoise. They can be found on either the Koikatsu Discord or IllusionSoft Discord.
