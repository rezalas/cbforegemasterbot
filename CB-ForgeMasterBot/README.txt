Author: Paul McDowell



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