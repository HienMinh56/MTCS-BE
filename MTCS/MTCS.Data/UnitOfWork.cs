using Microsoft.EntityFrameworkCore.Storage;
using MTCS.Data.Models;
using MTCS.Data.Repository;

namespace MTCS.Data
{
    public class UnitOfWork
    {
        private IDbContextTransaction _transaction;

        private MTCSContext context;
        private IncidentReportsRepository incidentReportsRepository;
        private IncidentReportsFileRepository incidentReportsFileRepository;
        private UserRepository userRepository;
        private InternalUserRepository internalUserRepository;
        private DriverRepository driverRepository;
        private TripRepository tripRepository;
        private TractorRepository tractorRepository;
        private TrailerRepository trailerRepository;
        private ContractRepository contractRepository;
        private ContractFileRepository contractFileRepository;
        private FuelReportRepository fuelReportRepository;
        private FuelReportFileRepository fuelReportFileRepository;
        private DeliveryReportRepository deliveryReportRepository;
        private DeliveryReportFileRepository deliveryReportFileRepository;
        private OrderRepository orderRepository; 
        private OrderFileRepository orderFileRepository; 
        private CustomerRepository customerRepository;

        public UnitOfWork()
        {
            context ??= new MTCSContext();
        }

        public UserRepository UserRepository
        {
            get
            {
                return userRepository ??= new UserRepository();
            }
        }
        public InternalUserRepository InternalUserRepository
        {
            get
            {
                return internalUserRepository ??= new InternalUserRepository();
            }
        }

        public DriverRepository DriverRepository
        {
            get
            {
                return driverRepository ??= new DriverRepository();
            }
        }
        public TripRepository TripRepository
        {
            get
            {
                return tripRepository ??= new TripRepository();
            }
        }

        public TractorRepository TractorRepository
        {
            get
            {
                return tractorRepository ??= new TractorRepository();
            }
        }

        public TrailerRepository TrailerRepository
        {
            get
            {
                return trailerRepository ??= new TrailerRepository();
            }
        }

        public IncidentReportsRepository IncidentReportsRepository
        {
            get
            {
                return incidentReportsRepository ??= new IncidentReportsRepository();
            }
        }

        public IncidentReportsFileRepository IncidentReportsFileRepository
        {
            get
            {
                return incidentReportsFileRepository ??= new IncidentReportsFileRepository();
            }
        }

        public ContractRepository ContractRepository
        {
            get
            {
                return contractRepository ??= new ContractRepository();
            }
        }

        public ContractFileRepository ContractFileRepository
        {
            get
            {
                return contractFileRepository ??= new ContractFileRepository();
            }
        }

        public FuelReportRepository FuelReportRepository
        {
            get
            {
                return fuelReportRepository ??= new FuelReportRepository();
            }
        }

        public FuelReportFileRepository FuelReportFileRepository
        {
            get
            {
                return fuelReportFileRepository ??= new FuelReportFileRepository();
            }
        }

        public DeliveryReportRepository DeliveryReportRepository
        {
            get
            {
                return deliveryReportRepository ??= new DeliveryReportRepository();
            }
        }

        public DeliveryReportFileRepository DeliveryReportFileRepository
        {
            get
            {
                return deliveryReportFileRepository ??= new DeliveryReportFileRepository();
            }
        }
        public OrderRepository OrderRepository
        {
            get
            {
                return orderRepository ??= new OrderRepository();
            }
        }

        public OrderFileRepository OrderFileRepository
        {
            get
            {
                return orderFileRepository ??= new OrderFileRepository();
            }
        }

        public CustomerRepository CustomerRepository
        {
            get
            {
                return customerRepository ??= new CustomerRepository();
            }
        }


        //    ////TO-DO CODE HERE/////////////////

        //    #region Set transaction isolation levels

        //    /*
        //    Read Uncommitted: The lowest level of isolation, allows transactions to read uncommitted data from other transactions. This can lead to dirty reads and other issues.

        //    Read Committed: Transactions can only read data that has been committed by other transactions. This level avoids dirty reads but can still experience other isolation problems.

        //    Repeatable Read: Transactions can only read data that was committed before their execution, and all reads are repeatable. This prevents dirty reads and non-repeatable reads, but may still experience phantom reads.

        //    Serializable: The highest level of isolation, ensuring that transactions are completely isolated from one another. This can lead to increased lock contention, potentially hurting performance.

        //    Snapshot: This isolation level uses row versioning to avoid locks, providing consistency without impeding concurrency. 
        //     */

        //    public int SaveChangesWithTransaction()
        //    {
        //        int result = -1;

        //        //System.Data.IsolationLevel.Snapshot
        //        using (var dbContextTransaction = context.Database.BeginTransaction())
        //        {
        //            try
        //            {
        //                result = context.SaveChanges();
        //                dbContextTransaction.Commit();
        //            }
        //            catch (Exception)
        //            {
        //                //Log Exception Handling message                      
        //                result = -1;
        //                dbContextTransaction.Rollback();
        //            }
        //        }

        //        return result;
        //    }

        //    public async Task<int> SaveChangesWithTransactionAsync()
        //    {
        //        int result = -1;

        //        //System.Data.IsolationLevel.Snapshot
        //        using (var dbContextTransaction = context.Database.BeginTransaction())
        //        {
        //            try
        //            {
        //                result = await context.SaveChangesAsync();
        //                dbContextTransaction.Commit();
        //            }
        //            catch (Exception)
        //            {
        //                //Log Exception Handling message                      
        //                result = -1;
        //                dbContextTransaction.Rollback();
        //            }
        //        }

        //        return result;
        //    }

        //    #endregion
        public async Task BeginTransactionAsync()
        {
            _transaction = await context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            await _transaction.CommitAsync();
        }

        public async Task RollbackTransactionAsync()
        {
            await _transaction.RollbackAsync();
        }
    }
}