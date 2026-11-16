A chess engine developed in C# from scratch, using optimized search algorithm. Engine is capable of analyzing positions, evaluating potential moves and choosing the strongest one after search is completed.

Features:
-user can select engine strength between beginner, intermediate and advanced
-user can also choose whether he wants to play with white pieces or black, and time on the clock
-all chess rules are correctly implemented (piece movement, castling, pawn promotion, en passant, etc...)
-move generation and validation
-position evaluation
-game tree search based on minimax algorithm
-plenty of advanced search optimization techniques

Technologies:
-C#
-.NET
-WPF

Screenshots:
<img width="782" height="433" alt="Screenshot 2026-11-16 090111" src="https://github.com/user-attachments/assets/296606a2-b139-44f9-9227-71831a87d775" />
<img width="1173" height="980" alt="Screenshot 2026-11-16 090127" src="https://github.com/user-attachments/assets/42f12e77-fe7f-44aa-84fd-639f56778695" />

Architecture:
Game logic is separated from graphical user interface. Folder views includes graphical representation of all the windows in this application, and graphical representation of chess board.
Folder gameLogic includes logical board representation as a 2-dimensional array, abstract class piece that is inherited by each chess piece with their specific rules, search algorithm and search optimization techniques.

Playing strength:
Engine is able to play at around 2100 elo skill level currently. Strength is approximated by testing the engine against other bots on site chess.com. It easily beats oppponents rated 2000 and below, but struggles against bots rated 2300 and higher.

Search algorithm:
Engine determines best move by searching through the game tree and finding move with the highest value. Each node of this tree is a potential position that could occur in future during the game, and every position is given a numerical value. Position is evaluated based on strength of each piece (for example queen=980, rook=500, knight=300, etc...) and their positional values, which is mostly handled by piece square tables. A move that has the best value for engine will be chosen. Millions of positions must be evaluated for each move, so some branches of the tree must be cut and discarded to speed up the process. Without optimisation techniques it would take several minutes or even hours for a single move for engine to make. Easiest trees to eliminate are the ones that cant impact the final decision. For example, engine will assume the good moves will be played, and not analyze ones that are clearly losing to reduce time complexity, which is very crucial for chess engines. Transposition tables are implemented to avoid calculating same positions multiple times. Order of the analyzed moves also matters, because if a move with highest value is evaluated very early, the more branches it will be possible to safely discard later and speed up the search. The entire point of these optimisations is to speed up the process that will return the best move. The quicker that process concludes, the deeper the engine will be able to search while being able to play the move with reasonable speed (few seconds per move usually).
