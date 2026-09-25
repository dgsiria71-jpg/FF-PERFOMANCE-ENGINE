namespace FFPerformanceEngine.Core.Models;

public enum ThemeMode { Clean, Dark }
public enum GameKind { None, FreeFire, FreeFireMax }
public enum GameState { Offline, Desktop, BlueStacksStarting, BlueStacksReady, GameStarting, Lobby, MatchPrep, Match, MatchEnd, PostMatch }
public enum GuardianMode { Conservative, Adaptive, Aggressive, MonitorOnly }
public enum AutoTunerMode { Adaptive, Deep }
public enum ProfileKind { Recommended, MaximumFps, LowestLatency, Stability, Quality, Custom }
public enum EvidenceLevel { Unknown, Estimated, Observed, Validated }
public enum ActionSafety { LiveSafe, LobbySafe, RestartRequired, WindowsRestart, Experimental, Blocked }
public enum HistoryEventKind { Optimization, Benchmark, Profile, Guardian, Snapshot, System, BlueStacks, Game, Expert, Restore }
