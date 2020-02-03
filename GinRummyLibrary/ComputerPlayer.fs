module ComputerPlayer

open Cards
open GinRummy

type Move = Gin | Knock | Continue

let ComputerPickupDiscard computerHand topDiscard possibleDeck =
    if Seq.isEmpty possibleDeck
    then true
    else
        let AvgDeadwoodOnPossibleDeck =
            possibleDeck 
            |> Seq.averageBy (fun card -> float (DeadwoodAfterDiscard (Seq.append computerHand [card])))
        AvgDeadwoodOnPossibleDeck > float (DeadwoodAfterDiscard (Seq.append computerHand [topDiscard]))

let ComputerMove newHand =
    if Deadwood newHand = 0
    then (Gin, None)
    else   
        let (card, deadwood) = DeadwoodAfterDiscardTuple newHand
        if deadwood = 0 then
            (Gin, Some card)
        elif deadwood <= 10 then
            (Knock, Some card) 
        else
            (Continue, Some card)

