using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite; // import per utilizzare gli oggetti Result con gli statu code
var builder = WebApplication.CreateBuilder(args);
// cosi al nostra applicazone sarà sempre dipendente dall'oggetto istanza della classe InMemoryTaskService
builder.Services.AddSingleton<ITaskService>(new InMemoryTaskService()); // qui inseriamo la nostra dipendenza nell'applicazione ( si usa sigleton perchè sarà dipendente per tutta la durata vitale dell'app)
var app = builder.Build();

var todos = new List<ToDo>(); // creiamo una lista di todos dove storare i todo

// MIDDLEWARE , RICORDATI CHE  RUNNANO IN ORDINE
// aggiungo il middleware per reindirizzare un utente che digita task a todos riscrivendo l'url
// tutti i mid fanno parte dell'application pipeline e vengono eseguiti fra la richiesta e la risposta in ogni singolo endpoin
// tutti i mid mettono a disposizione la possibilità di riscrivere un opzione,in questo caso utilizzo l'opzione di riscrittura
app.UseRewriter(new RewriteOptions().AddRedirect("task/(.*)", "todos/$1"));

// adesso creo un mid custom
app.Use(async (context, next) =>
{ //  context rappresenta la Request e la Response correnti
  // ci permette di accedere quindi all'oggetto richiesta e a quello risposta
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Started"); // loggo alcune informazioni della richiesta e l'orario
    await next(context);
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] finished");// riloggo alcune informazioni della richiesta e l'orario alla fine della pipeline
});



// CRAIAMO I NOSTRI END POINT PER LA CRUD UTILIZZANDO I METODI DEL SERVIZIO IMPEMENTATO

app.MapGet("/todos", (ITaskService service) => service.GetToDos()); // primo get , ritorna tutti gli elementi della todo list

app.MapGet("/todos/{id}", Results<Ok<ToDo>, NotFound> (int id,ITaskService service) =>
{ // tipizzo il ritorno del mio handler , tornerà 404 se non trova nulla
  // altrimenti tornerà status 200 con il todo corrispondente

    //Restituisce il singolo elemento di una sequenza o un valore predefinito se la sequenza è vuota. Questo metodo genera un'eccezione se esiste più di un elemento nella sequenza.
    var targetTodo = service.GetToDoById(id); // tipo il find di un array , trova l'elemento che soddisfa una determinata condizione
    
    return targetTodo is null
        ? TypedResults.NotFound() // se non trovo nulla 404
        : TypedResults.Ok(targetTodo); // altrimenti invio il todo
});

app.MapPost("/todos", (ToDo task,ITaskService service) => // crea un elemento con un id
{
    service.AddToDo(task);
    Console.WriteLine(todos);

    return TypedResults.Created("/todos/id", task); // la risposta sarà uno status code 201 CREATED 
}).AddEndpointFilter(async (context, next) => // filtro di validazione , funziona come un middleware, più o meno come express validator
{
    var taskArgument = context.GetArgument<ToDo>(0); // prende il primo argomento della post, in questo caso il todo
    var error = new Dictionary<string, string[]>(); // creiamo un dizionario dove storare tutti i nostri eventuali errori nella validazione

    if (taskArgument.DueDate < DateTime.UtcNow)
    { // il primo errore potrebbe essere una data inferiore a quella odierna

        error.Add(nameof(ToDo.DueDate), ["cannot have due date in the past"]); // inserisco l'errore nel dizionario

    }
    if (taskArgument.IsComplete)
    { //secondo errore, controllo se il todo è gia stato completato

        error.Add(nameof(ToDo.IsComplete), ["cannot have a todo that is already complete"]); // inserisco l'errore nel dizionario

    }
    if (error.Count > 0) // se ci sono errori nel dizionario la richiesta sarà non valida
    { 

        return Results.ValidationProblem(error); // ritorno all'utente un errore 401 e il dizionario degli errori

    }

    return await next(context); // vado al prossiomo filter o all'handler finale

});






app.MapDelete("/todos/{id}", (int id,ITaskService service) =>
{
    service.DeletToDoById(id); // rimuove l'elemento con id specifica dalla lista
    return TypedResults.NoContent(); // Produces a Status204NoContent response.

});

app.Run();


public record ToDo(int Id, string Name, DateTime DueDate, bool IsComplete); // un record permette la creazione di un tipo, utile per il nostro todolist


// piuttosto che utilizzare tutta la logica all'interno dei nostri end point per gestire la richiesta e la risposta,
// possiamo CENTRALIZZARE LA LOGICA , utilizzando la DEPENDENCY INJECTION nei nostri end point:
// LE DIPENDENZE SONO OGGETTI DA CUI ALTRI OGGETTI POSSONO DIPENDERE , OPPURE POSSONO ESSERE CHIAMATI SERVIZI (strored in the service container)

// adesso implementeremo il nostro primo service!!!!

// Definiamo un interfaccia che rapprensenta le funzionalità comuni del nostro servizio

 interface ITaskService{
    ToDo? GetToDoById(int id); // creo i metodi che impementerò nel nostro servizio
    List<ToDo> GetToDos();

    void DeletToDoById(int id);

    ToDo AddToDo(ToDo task);
 }

// adesso creo la classe per il nostro servizio

class InMemoryTaskService : ITaskService 
{
    private readonly List<ToDo> _todos = []; // implemento la nostra interfaccia nella classe
    public ToDo AddToDo(ToDo task)
    {
        _todos.Add(task);
        return task;
    }

    public void DeletToDoById(int id)
    {
        _todos.RemoveAll(t => id == t.Id);
    }

    public ToDo? GetToDoById(int id)
    {
        
        return _todos.SingleOrDefault(t => id == t.Id);
    }

    public List<ToDo> GetToDos()
    {
        return _todos;
    }
}

// DOPO AVER CREATO IL SERVIZIO , DOBBIAMO INSERIRE LA DIPENDENZA NELLA NOSTRA APP NELLA RIGA 5 UTILIZZANDO UN BUILDER

