using Converj.Attributes;

namespace Converj.Example;

internal class Recipes
{
    public void Run()
    {
        var burrito = Meal
            .WithCheese("Cheddar")
            .WithTortilla("Flour")
            .WithRice("White")
            .WithSalsa("Mild")
            .MakeBurrito();
        
        var burritoBowl = Meal
            .WithCheese("Cheddar")
            .WithRice("Brown")
            .WithSalsa("Mild")
            .MakeBurritoBowl();
        
        var grilledCheese = Meal
            .WithCheese("Swiss")
            .WithBread("Sourdough")
            .MakeGrilledCheese();
        
        var cheeseburger = Meal
            .WithCheese("American")
            .WithPatty("Beef")
            .WithBun("Sesame")
            .MakeCheeseburger(); 
    }
}

/// <summary>
/// Demonstrates trie-based parameter merging: shared ingredients form a common
/// prefix in the decision tree, then branch into meal-specific parameters.
///
/// Trie structure:
///
///   WithCheese ──┬── WithSalsa ── WithRice ── MakeBurritoBowl
///                ├── WithTortilla ── WithSalsa ── WithRice ── MakeBurrito
///                ├── WithBread ── MakeGrilledCheese
///                └── WithPatty ── WithBun ── MakeCheeseburger
///
/// All four meals start with cheese, then diverge by their next ingredient.
/// Burrito and BurritoBowl share cheese + tortilla + salsa before splitting.
/// </summary>
[FluentRoot(ReturnType = typeof(Meal), TerminalVerb = "Make")]
internal abstract partial record Meal;

[FluentTarget<Meal>]
internal record Burrito(string Cheese, string Tortilla, string Rice, string Salsa) : Meal;

[FluentTarget<Meal>]
internal record BurritoBowl(string Cheese, string Rice, string Salsa) : Meal;

[FluentTarget<Meal>]
internal record GrilledCheese(string Cheese, string Bread) : Meal;

[FluentTarget<Meal>]
internal record Cheeseburger(string Cheese, string Patty, string Bun) : Meal;
