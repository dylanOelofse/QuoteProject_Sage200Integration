using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace IntegrationPollingService
{
    public class Service
    {
        private readonly System.Timers.Timer _timer;
        private readonly object _lock = new object();

        private bool isBusy = false;

        public Service()
        {
            _timer = new System.Timers.Timer(10000) { AutoReset = true };
            _timer.Elapsed += TimerElapsed;
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)

        {

            if (!Monitor.TryEnter(_lock))

                return;


            try
            {
                SyncSageQuotes.SageToCrm();
                SyncSageQuotes.crmToStaging();
                SyncSageQuotes.stagingToSage();
                SyncSageQuotes.updateQuotes();
                SyncSageQuotes.reconcileSageQuotes();
                //SyncSageQuotes.deleteQuote();  //SalesOrders cannot be deleted from SDK only in pastel Evo
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());   // log his into db
            }

            finally
            {

                Monitor.Exit(_lock);

            }
        }


        public void Start()
        {
            try
            {
                _timer.Start();
            }

            catch (Exception ex)

            {

            }
        }

        public void Stop()

        {
            _timer.Stop();
        }
    }
}

