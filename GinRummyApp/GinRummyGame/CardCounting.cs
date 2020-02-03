using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QUT.GinRummyGame
{
    class CardCounting
    {
        public HashSet<Cards.Card> ExcludedCards { get; set; }
        public CardCounting(IEnumerable<Cards.Card> computerHand, Cards.Card topOfDiscard)
        {
            
        }
    }
}
