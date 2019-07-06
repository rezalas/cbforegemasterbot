Author: Paul McDowell


Thanks:

	This project exists in great part due to the efforts of hard working men and women around the world creating and sharing their
efforts with all of us. I'd like to thank the team working on Discord.Net, CoreRCON, Serilog, and certainly not least the .Net Core
team and .Net Foundation. Their work powers this bot to a great degree and as such should be recognized.
	

Requirements:

	In order to properly build this bot, you need access to the CoreRCON files which should be downloaded to your computer somewhere.
You should be able to find the package on github and save it to your computer in a location of your choosing. Once you have, 
modify the hint path for it in CB-ForgeMasterBot.csproj which looks like the following:

  <ItemGroup>
    <Reference Include="CoreRCON">
      <HintPath>E:\users\paul\Documents\GitHub\CoreRCON\src\CoreRCON\bin\Release\netstandard2.0\CoreRCON.dll</HintPath>
    </Reference>
  </ItemGroup>

Change the hintpath to target your actual path for the release file, otherwise you're going to have a few issues building.