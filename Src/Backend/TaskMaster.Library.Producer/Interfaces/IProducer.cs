using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMaster.Library.Producer.Interfaces
{
    public interface IProducer
    {
        Task ProduceAsync<T>(T data); 
    }
}
