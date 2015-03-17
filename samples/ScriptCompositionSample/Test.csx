#r SampleContracts.dll
using SampleContracts;

[Export(typeof(IGreeter))]
public class Greeter : IGreeter {
  public void Greet(string message) {
    Console.WriteLine(message);
  }
}