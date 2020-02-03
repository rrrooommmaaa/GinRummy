module GinRummy

open Cards

// Add helper functions to help compute Deadwood function
let RankPoints = 
    [
        Ace, 1;
        Two, 2;
        Three, 3;
        Four, 4;
        Five, 5;
        Six, 6;
        Seven, 7;
        Eight, 8;
        Nine, 9;
        Ten, 10;
        Jack, 10;
        Queen, 10;
        King, 10
    ]
    |> Map.ofList

let RankIndexes = 
    [
        Ace, 1;
        Two, 2;
        Three, 3;
        Four, 4;
        Five, 5;
        Six, 6;
        Seven, 7;
        Eight, 8;
        Nine, 9;
        Ten, 10;
        Jack, 11;
        Queen, 12;
        King, 13
    ]
    |> Map.ofList

let GetRankIndex (rank:Rank) = 
    Map.find rank RankIndexes

let IsPreviousRank (rank:Rank) (prevRank:Rank) =
    GetRankIndex rank = GetRankIndex prevRank + 1

let IsNextRank  (rank:Rank) (nextRank:Rank) =
    GetRankIndex rank = GetRankIndex nextRank - 1

let HandExceptCards (cards:Hand) (hand:Hand) =
    hand 
    |> Seq.choose (fun card -> if (Seq.exists ((=) card) cards) then None else Some(card)) 

type Run = Card seq

let rec ExtendRun (run:Run) (hand:Hand) = 
    let FindPreviousHead (head:Card) (hand:Hand) = 
        TakeSameSuit head hand
        |> Seq.tryFind (fun card -> IsPreviousRank head.rank card.rank)
    let FindNextTail (tail:Card) (hand:Hand) = 
        TakeSameSuit tail hand
        |> Seq.tryFind (fun card -> IsNextRank tail.rank card.rank)

    let nextTail = FindNextTail (Seq.last run) hand

    if nextTail = None
    then
        match FindPreviousHead (Seq.head run) hand with
        | Some card -> ExtendRun (Seq.append [card] run) (HandExceptCards [card] hand)
        | None -> run
    else
        match nextTail with
        | Some card -> ExtendRun (Seq.append run [card]) (HandExceptCards [card] hand)
        | None -> run

let PipValue card =
    Map.find card.rank RankPoints

let InRun hand card =
    ExtendRun [card] (HandExceptCards [card] hand)
    |> Seq.length >= 3

let InSet hand card =
    GetSet card hand
    |> Seq.length >= 3


let CountPoints (hand:Hand) =
    hand
    |> Seq.map PipValue
    |> Seq.sum 

let rec Deadwood (hand:Hand) = 
    if Seq.isEmpty hand then
        0
    else
        Seq.min([(DeadwoodWithRun hand);(DeadwoodWithSet hand)])
and DeadwoodWithRun (hand:Hand) =
    hand
    |> Seq.map (fun card -> 
        let MaxRun = ExtendRun [card] (HandExceptCards [card] hand)
        if Seq.length MaxRun >= 3 
        then Deadwood (HandExceptCards MaxRun hand)
        else CountPoints hand)
    |> Seq.min
and DeadwoodWithSet (hand:Hand) =
    hand
    |> Seq.map (fun card ->
        let cardSet = GetSet card hand
        if Seq.length cardSet >= 3 
        then Deadwood (HandExceptCards cardSet hand)
        else CountPoints hand)
    |> Seq.min

let DeadwoodAfterDiscardTuple hand =
    hand
    |> Seq.map (fun card -> 
        (card, Deadwood (HandExceptCards [card] hand)))
    |> Seq.minBy (fun (card, deadwood) -> deadwood)

let DeadwoodAfterDiscard hand =
    let (card, deadwood) = (DeadwoodAfterDiscardTuple hand)
    deadwood

let Score (firstOut:Hand) (secondOut:Hand) =
    let firstDeadwood = 
        if Seq.length firstOut = 11
        then DeadwoodAfterDiscard firstOut
        else Deadwood firstOut
    let secondDeadwood = Deadwood secondOut
    if firstDeadwood = 0 then
        25 + secondDeadwood
    elif firstDeadwood < secondDeadwood then
        secondDeadwood - firstDeadwood
    else
        -25 - firstDeadwood + secondDeadwood

