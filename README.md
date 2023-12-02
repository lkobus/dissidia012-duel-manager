# Dissidia 012 Duel Manager

> ℹ️ With this you can manage a dissidia duodecim match with **map bans** and **blind pick** using your own discord. It's also possible adapt this to others fighting games.

## Requirements

- Dotnet 7.0 runtime
    - https://dotnet.microsoft.com/en-us/download/dotnet/7.0
- Already created a bot and added to your server
    - Create your bot here -> https://discord.com/developers/applications
    - Invite to your server

### Initial configuration

You need to define `app-config.json` I'll explain bellow what each fields does

- language
    - For now we only have en and pt, you can expand options in languages folder 
- guildId
    - Its the id of your server, right click on your server icon or name and copy the id
- clientId
    - Its the bot client Id you need to open the bot page in https://discord.com/developers/applications
    - Oauth option and copy the clientId field
- player1RoleId & player2RoleId
    - You need to create a role to manage who is going to play player1 and player2
    - After that you can fill each option right clicking on the role in your configuration and copy the ID.
- player1ChannelId & player2ChannelId
    - the same idea  as the roles, but its the channel that the bot will ask individually for each players the picks and bans
    - You can make this chat private allow the TOs to join and you can use this a directly way of contacting a specific player.
- overlayFullPath
    - Every time a update occurs on the bot it will update files in this folder, this folder can be used along with obs studio or streamlabs to show in real time each player action.

## How to run

With the dotnet runtime installed you just need to run the follow command to bring the bot online

`> dotnet TorneioBot.dll`

You should see some log like this
```
Slash commands updated!
13:32:31 Discord     Discord.Net v3.12.0 (API v10)
13:32:31 Gateway     Connecting
13:32:31 Gateway     You're using the GuildPresences intent without listening to the PresenceUpdate event, consider removing the intent from your config.
13:32:31 Gateway     You're using the GuildScheduledEvents gateway intent without listening to any events related to that intent, consider removing the intent from your config.
13:32:31 Gateway     You're using the GuildInvites gateway intent without listening to any events related to that intent, consider removing the intent from your config.
13:32:31 Gateway     Connected
13:32:32 Gateway     Ready
13:32:32 Gateway     Ready
```

After this
- The bot should be online in your discord
- The slash commands is always updated on bot startup so the bots commands must be avaliable in the discord.

### Bots command

**Command** `/tournament_012_new_match`
```
Parameters
- player-1 & player-2 (discord mention)
    - Discord mention for both players
- rounds (integer)
    - Total number of rounds avaliable
    - If its a BO3 specify 3 rounds
    - If its a FT3 specify 5 rounds
- match (string)
    - This will be used in overlay and messages to inform your players
    - If you are using a brackets system you can use the match number.
    - If you are hosting a duel you can write any title here (like FT3).
- bracket (string)
    - Same as match 
- host (discord mention)
    - The player who will host the lobby
    - When you start the match the bot will announce in a channel the side each player must go and who needs to be the host.
- announcement-channel (channel-name)
    - When you start the match the bot will send a message to call players to the group battle and setup the online room    
```
**What the command does**

- Remove role player-1 and player-2 from all members
- Add player-1 and player-2 role to mentions informed in parameters
- Send a message to tell who is the host each side of player (A or B) in the channel specified in parameters
- Reset all scores, overlay and variables.
- Calculate how many rounds are possible and setup the orders of actions for the match.
---

**Command** `/tournament_012_start_round`

**What the command does**
- If its round 1
    - Will flip coin
    - Who wins the flip coins start banning the map
    - Send the map ban question to the player and initiate the round setup.
- If its round 2 or +
    - The player who won the last match will start banning
    - Send the map ban question to the player and initiate the round setup.
---
**Command** `/tournament_012_set_winner`

**Parameters**
- winner
    - discord mention for the player who won the round

**What the command does**
- if its not the last match
    - Will send discord message for both player of the actual score
    - Increment round
- if its the last match
    - Bot will send message that the set has ended
    - Bot will congratulate the winner
    - Update overlay with winner and set end.
