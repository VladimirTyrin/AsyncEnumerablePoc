using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

await using (var writeContext = new FooContext())
{
    var foos = writeContext.GetSet();
    foreach (var foo in Enumerable.Range(1, 10).Select(x => new Foo { Value = x * 2 }))
    {
        foos.InsertOnSubmit(foo);
    }
    writeContext.SaveChanges();
}

await using (var readContext = new FooContext())
{
    var foos = readContext.GetSet();
    
    Console.WriteLine(await foos.Where(f => f.Value > 8).CountAsync());
    await foreach (var foo in foos.AsAsyncEnumerable())
    {
        Console.WriteLine($"{foo.Id}: {foo.Value}");
    }
}

[PrimaryKey(nameof(Id))]
public class Foo
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public int Value { get; set; }
}

public class FooContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(databaseName: "FooDb");
    }
    
    public DbSet<Foo> Foos { get; set; }

    public IDbEntitySet<Foo> GetSet() => new EfDbEntitySet<Foo>(Foos);
}

public interface IDbEntitySet<TEntity> : IQueryable<TEntity>
    where TEntity : class
{
    void InsertOnSubmit(TEntity entity);
    void DeleteOnSubmit(TEntity entity);
    void Attach(TEntity entity);
}

internal class EfDbEntitySet<TEntity> : IDbEntitySet<TEntity>, IAsyncEnumerable<TEntity>
    where TEntity : class
{
    private readonly DbSet<TEntity> _dbSet;
    private readonly IQueryable<TEntity> _dbSetIQueryable;

    public EfDbEntitySet(DbSet<TEntity> dbSet)
    {
        _dbSet = dbSet;
        _dbSetIQueryable = dbSet;
    }

    public IEnumerator<TEntity> GetEnumerator() => _dbSetIQueryable.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _dbSetIQueryable.GetEnumerator();

    public IAsyncEnumerator<TEntity> GetAsyncEnumerator(CancellationToken cancellationToken = new())
        => _dbSet.GetAsyncEnumerator(cancellationToken);

    public Type ElementType => _dbSetIQueryable.ElementType;
    public Expression Expression => _dbSetIQueryable.Expression;
    public IQueryProvider Provider => _dbSetIQueryable.Provider;

    public void InsertOnSubmit(TEntity entity) => _dbSet.Add(entity);

    public void DeleteOnSubmit(TEntity entity) => _dbSet.Remove(entity);

    public void Attach(TEntity entity) => _dbSet.Attach(entity);
}