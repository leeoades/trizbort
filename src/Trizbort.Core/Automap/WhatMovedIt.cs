namespace Trizbort.Automap;

/// <summary>
///   A command the player typed in a room, with the command it answered when the game asked a question first ("pull
///   peg", "[Which peg do you mean...?]", then "second").
/// </summary>
public sealed record DoneInRoom(string Command, string Asked = null)
{
  /// <summary>As a join's label: "pull peg, second". Automap follows it for "pull peg" then "second", or "pull second peg".</summary>
  public string Label => Asked != null ? Asked + ", " + Command : Command;

  /// <summary>As listed for the user: "pull peg → second".</summary>
  public string Display => Asked != null ? Asked + " → " + Command : Command;
}

/// <summary>
///   The answer to "what moved it?": the command that did (the label for the join between its stops), or
///   <see cref="ByItself" />: nothing the player did (it moves by itself, or time passed).
/// </summary>
public sealed record WhatMovedItChoice(string Command, bool ByItself = false)
{
  public static WhatMovedItChoice Itself { get; } = new(null, true);
}
