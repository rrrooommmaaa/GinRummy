using System;
using System.Collections.Generic;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using QUT.Extensions;

namespace QUT
{
    class ViewModel: INotifyPropertyChanged
    {
        public ObservableCollection<Cards.Card> HumanCards { get; private set; }
        public ObservableCollection<Cards.Card> ComputerCards { get; private set; }
        public ObservableCollection<Cards.Card> Discards { get; private set; }
        public ObservableCollection<Cards.Card> RemainingDeck { get; private set; }

        public InteractionRequest<INotification> NotificationRequest { get; private set; }

        public ICommand ButtonCommand { get; set; }
        public ICommand DiscardCardFromHandCommand { get; set; }
        public ICommand TakeCardFromDiscardPileCommand { get; set; }
        public ICommand TakeCardFromDeckCommand { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        // Any card that appear among the computer cards or in the discard pile
        // will never return to the deck, so we just accumulate the cards we see
        private HashSet<Cards.Card> _excludedCards;

        private int computerScore = 0;
        public int ComputerScore
        {
            get
            {
                return computerScore;
            }
            private set
            {
                computerScore = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("ComputerScore"));
            }
        }

        private int playerScore = 0;
        public int PlayerScore
        {
            get
            {
                return playerScore;
            }
            private set
            {
                playerScore = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("PlayerScore"));
            }
        }

        private bool playerTurn;

        public bool PlayerTurn
        {
            get
            {
                return playerTurn;
            }
            private set
            {
                playerTurn = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("PlayerTurn"));
            }
        }

        public ViewModel()
        {
            TakeCardFromDiscardPileCommand = new DelegateCommand<Cards.Card>(TakeCardFromDiscardPile, TakeCardCanExecute);
            DiscardCardFromHandCommand = new DelegateCommand<Cards.Card>(DiscardCardFromHand, DiscardCardFromHandCanExecute);
            TakeCardFromDeckCommand = new DelegateCommand<Cards.Card>(TakeCardFromDeck, TakeCardCanExecute);

            ButtonCommand = new DelegateCommand(ButtonClick);
            NotificationRequest = new InteractionRequest<INotification>();

            HumanCards = new ObservableCollection<Cards.Card>();
            ComputerCards = new ObservableCollection<Cards.Card>();
            Discards = new ObservableCollection<Cards.Card>();
            RemainingDeck = new ObservableCollection<Cards.Card>();
            _excludedCards = new HashSet<Cards.Card>();

            HumanCards.CollectionChanged += HumanCards_CollectionChanged;
            Discards.CollectionChanged += Discards_CollectionChanged;
            ComputerCards.CollectionChanged += ComputerCards_CollectionChanged;

            Deal();
        }

        private void ComputerCards_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                _excludedCards.UnionWith(e.NewItems.Cast<Cards.Card>());
        }

        private void Discards_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                _excludedCards.UnionWith(e.NewItems.Cast<Cards.Card>());
        }

        private bool DiscardCardFromHandCanExecute(Cards.Card arg)
        {
            return PlayerTurn && HumanCards.Count == 11;
        }

        private bool TakeCardCanExecute(Cards.Card arg)
        {
            return PlayerTurn && HumanCards.Count == 10;
        }

        private async void Deal()
        {
            Discards.Clear();
            RemainingDeck.Clear();
            _excludedCards.Clear();
            ComputerCards.Clear();
            HumanCards.Clear(); 
            var deck = Cards.Shuffle(Cards.FullDeck);

            foreach (var card in deck)
            {
                RemainingDeck.Add(card);
                await Task.Delay(1);
            }

            for (int i = 0; i < 10; i++)
            {
                ComputerCards.Add(DrawTopCardFromDeck());
                await Task.Delay(30);
                HumanCards.Add(DrawTopCardFromDeck());
                await Task.Delay(30);
            }

            Discards.Add(DrawTopCardFromDeck());
            PlayerTurn = true;
        }

        private IEnumerable<Cards.Card> PossibleDeck
        {
            get { return Cards.FullDeck.Except(_excludedCards); }
        }

        private Cards.Card DrawTopCardFromDeck()
        {
            var top = RemainingDeck[RemainingDeck.Count - 1];
            RemainingDeck.Remove(top);
            return top;
        }

        private void TakeCardFromDeck(Cards.Card card)
        {
            RemainingDeck.Remove(card);
            HumanCards.Add(card);
        }

        private void TakeCardFromDiscardPile(Cards.Card p)
        {
            Discards.Remove(p);
            HumanCards.Add(p);
        }

        async private void DiscardCardFromHand(Cards.Card p)
        {
            PlayerTurn = false;
            HumanCards.Remove(p);
            Discards.Add(p);
            await ComputerMove();
        }

        private void DiscardCardFromComputerHand(Cards.Card p)
        {
            ComputerCards.Remove(p);
            Discards.Add(p);
        }

        private async Task ComputerMove()
        {
            bool pickupDiscard = !RemainingDeck.Any();
            if (!pickupDiscard)
            {
                ComputerStatus = "Computer is thinking where to pick from...";
                pickupDiscard = await Task.Run(() => ComputerPlayer.ComputerPickupDiscard(ComputerCards,
                    Discards.Last(), PossibleDeck));
            }
            if (pickupDiscard)
            {
                TakeCardFromDiscardPileForComputer();
            }
            else
            {
                TakeCardFromDeckForComputer();
            }

            ComputerStatus = "Computer has picked up a card from the " + (pickupDiscard ? "discard pile" : "deck");
            await Task.Delay(1500);

            ComputerStatus = "Computer is deciding what to discard...";
            var move = await Task.Run(() => ComputerPlayer.ComputerMove(ComputerCards));
            ComputerStatus = string.Empty;
            if (move.Item1.IsGin || move.Item1.IsKnock)
            {
                await FinishRound(false);
            }
            else
            {
                DiscardCardFromComputerHand(move.Item2.Value);
                PlayerTurn = true;
                ComputerStatus = "Your turn.";
            }
        }

        private void TakeCardFromDiscardPileForComputer()
        {
            var card = Discards.Last();
            Discards.Remove(card);
            ComputerCards.Add(card);
        }


        private void TakeCardFromDeckForComputer()
        {
            var card = DrawTopCardFromDeck();
            ComputerCards.Add(card);
        }

        async private void HumanCards_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            HumanDeadwood = "Calculating ...";
            // this might take a while, so let's do it in the background
            var copyOfHumanHand = HumanCards.ToList();
            int deadwood = await Task.Run(() => GinRummy.Deadwood(copyOfHumanHand));
            HumanDeadwood = "Deadwood: " + deadwood;
        }

        private string humanDeadwood;

        public string HumanDeadwood 
        { 
            get
            {
                return humanDeadwood;
            }
            private set
            {
                humanDeadwood = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("HumanDeadwood"));
            }
        }

        private string computerStatus;

        public string ComputerStatus
        {
            get
            {
                return computerStatus;
            }
            private set
            {
                computerStatus = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("ComputerStatus"));
            }
        }


        private void RaiseNotification(string msg, string title)
        {
            NotificationRequest.Raise(new Notification { Content = msg, Title = title });
        }

        async private Task FinishRound(bool isHumanFirstOut)
        {
            int score = await Task.Run(() => GinRummy.Score(isHumanFirstOut ? HumanCards : ComputerCards, 
                isHumanFirstOut ? ComputerCards : HumanCards));

            if (!isHumanFirstOut)
                score = -score;

            if (score < 0)
            {
                RaiseNotification(string.Format("Computer scored {0} points", -score), "Loss");
                ComputerScore -= score; // computer has negative points
            }
            else
            {
                PlayerScore += score;
                RaiseNotification(string.Format("Congratulations: you scored {0} points", score), "Win");
            }

            if (PlayerScore >= 100)
            {
                RaiseNotification("Player has won the game!", "Win");
                ComputerStatus = "Game over.";
            }
            else if (ComputerScore >= 100)
            {
                RaiseNotification("Computer has won the game!", "Loss");
                ComputerStatus = "Game over.";
            }
            else
            {
                Deal();
            }
        }

        private bool clicked = false;
        async private void ButtonClick()
        {
            if (clicked)
                return;
            clicked = true;
            if (!PlayerTurn)
                RaiseNotification("Not your turn!", "Error");
            else if (HumanCards.Count != 11)
                RaiseNotification("You should knock after taking a card!", "Error");
            else
            {
                int deadwood = await Task.Run(() => GinRummy.DeadwoodAfterDiscard(HumanCards));
                if (deadwood > 10)
                    RaiseNotification("You cannot knock if your deadwood is more than 10!", "Error");
                else
                {
                    await FinishRound(true);
                }
            }
            clicked = false;
        }
    }
}