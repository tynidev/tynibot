# Discord Rocket League Mafia Bot
A Discord bot that will let you create your own Rocket League Mafia games!

## How Mafia Works in Rocket League

### Traditional RL Mafia
The traditional RL Mafia format, as created by SunlessKhan: https://www.youtube.com/watch?v=nZjNx7UlqWY.
There are 5 villagers and 1 mafia for a 3v3 game (chosen randomly). The mafia's role is to lose the game, but discretely.
At the end of the game, players guess who was the mafia, and the mafia reveals themself at the end. Anyone who guessed the mafia
correctly would get 1 point, and if the mafia lost they would get 3 points.

### Improved RL Mafia
To make the game more fair, the rules and scoring was slightly adjusted. Now, instead of 1 mafia, there's 2 mafia.
This makes the game slightly more interesting in a 3v3 setting, as there's now more than one person to blame, and there
could potentialy be one mafia on each team. The scoring works as follows:

Villagers would get 1 point for a team win, 2 points for guessing each mafia.
Mafia would get 3 points for losing, 2 points for not being guessed as mafia (one point deduction per guess on them).

### Alternative Formats
- Instead of 1 mafia, it was tested that 1 mafia on each team made the game more interesting, as the mafia's had to compete against
each other to win. The idea would be to pick teams first and then assign a mafia to each team.
- If there's an odd number of people, instead of swapping out players each round (say 5 or 7 people), there would be an extra player on
one of the teams: the Joker. The Joker's main goal would be to try to tie the game, and scoring for the Joker would be
2 points for overtime, 3 points max for being guessed as mafia (one point per guess).

## To Use
It's recommended to use the help command first for more information once you get your mafia bot running.

`!mafia new <num of mafias> <@player1> <@player2>`

`!mafia vote <@mafia1> <@mafia2>`

`!mafia get`

`!mafia help`

## To Run

See here on how to get this bot running on your server:
https://www.youtube.com/watch?v=5sfdBQAjLpM
