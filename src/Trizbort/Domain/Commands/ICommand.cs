using Trizbort.UI.Controls;

namespace Trizbort.Domain.Commands;

public interface IParameterizedCommand<T, TValue> {
  T Execute(TValue value);
}

public interface ICommand<T> {
  T Execute();
}

public interface IParameterizedCommand<TV> {
  void Execute(TV value);
}

public interface ICanvasCommand<T, TV> {
  T Execute(Canvas canvas, TV value);
}

public interface ICanvasCommand<TV> {
  void Execute(Canvas canvas, TV value);
  void Execute(Canvas canvas, TV value, object other);
}