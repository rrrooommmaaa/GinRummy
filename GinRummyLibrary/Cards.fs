module Cards

open System

type Suit = Spades | Clubs | Hearts | Diamonds
type Rank = Ace | Two | Three | Four | Five | Six | Seven | Eight | Nine | Ten | Jack | Queen | King
type Card = { suit: Suit; rank: Rank}

type Hand = Card seq
type Deck = Card seq

let AllSuits = [ Spades; Clubs; Hearts; Diamonds ]
let AllRanks = [ Ace; Two; Three; Four; Five; Six; Seven; Eight; Nine; Ten; Jack; Queen; King ]

let allCards = 
    seq { 
        for s in AllSuits do
            for r in AllRanks do
                yield {suit=s; rank=r}
    }

let FullDeck = 
    allCards

let random = new Random()

let Shuffle (deck:Deck) = 
    let ShuffleInPlace (array : 'a[]) =
        let swap i j =
            let temp = array.[i]
            array.[i] <- array.[j]
            array.[j] <- temp
        let len = array.Length
        [0..len-2] |> Seq.iter(fun index -> 
            swap index (random.Next(index, len)))
        array
    deck
    |> Seq.toArray
    |> ShuffleInPlace
    |> Array.toSeq

// Add other functions here related to Card Games ...
let TakeSameSuit (card:Card) (hand:Hand) =
    hand
    |> Seq.choose (fun x -> if x <> card && x.suit = card.suit then Some(x) else None)

let GetSet (card:Card) (hand:Hand) =
    hand
    |> Seq.choose (fun x -> if x.rank = card.rank then Some(x) else None)

let CheckDuplicates cards =
    let duplicates = cards |> Seq.groupBy id |> Seq.map snd |> Seq.exists (fun s -> (Seq.length s) > 1)
    if (duplicates) then
        raise (new System.Exception "duplicates found!");