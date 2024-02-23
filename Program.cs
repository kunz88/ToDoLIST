using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite; // import per utilizzare gli oggetti Result con gli statu code

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var todos = new List<ToDo>(); // creiamo una lista di todos dove storare i todo

// aggiungo il middleware per reindirizzare un utente che digita task a todos riscrivendo l'url
// tutti i mid fanno parte dell'application pipeline e vengono eseguiti fra la richiesta e la risposta in ogni singolo endpoin
// tutti i mid mettono a disposizione la possibilità di riscrivere un opzione,in questo caso utilizzo l'opzione di riscrittura
app.UseRewriter(new RewriteOptions().AddRedirect("task/(.*)","todos/$1"));

app.MapGet("/todos", () =>todos); // primo get , ritorna tutti gli elementi della todo list


app.MapPost("/todos", (ToDo task) => // crea un elemento con un id
{
    todos.Add(task);
    Console.WriteLine(todos);

    return TypedResults.Created("/todos/id", task); // la risposta sarà uno status code 201 CREATED 
});




app.MapGet("/todos/{id}", Results<Ok<ToDo>, NotFound> (int id) =>
{ // tipizzo il ritorno del mio handler , tornerà 404 se non trova nulla
  // altrimenti tornerà status 200 con il todo corrispondente

    //Restituisce il singolo elemento di una sequenza o un valore predefinito se la sequenza è vuota. Questo metodo genera un'eccezione se esiste più di un elemento nella sequenza.
    var targetTodo = todos.SingleOrDefault(t => id == t.Id); // tipo il find di un array , trova l'elemento che soddisfa una determinata condizione
    return targetTodo is null
        ? TypedResults.NotFound() // se non trovo nulla 404
        : TypedResults.Ok(targetTodo); // altrimenti invio il todo
});

app.MapDelete("/todos/{id}" , (int id) =>{
    todos.RemoveAll(t => id == t.Id); // rimuove l'elemento con id specifica dalla lista
    return TypedResults.NoContent(); // Produces a Status204NoContent response.

});

app.Run();


public record ToDo(int Id, string Name, DateTime DueDate, bool IsComplete); // un record permette la creazione di un tipo, utile per il nostro todolist