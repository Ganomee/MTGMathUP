---
title: "MTG App: User Flows"
---

# User Flows

This document provides example **step-by-step interaction** flows for our main features—derived from **personas** and **storyboards**.

## Table of Contents
1. [Match Reporter Flow](#match-reporter-flow)
2. [Mulligan Tool Flow](#mulligan-tool-flow)
3. [Sideboarding Tool Flow](#sideboarding-tool-flow)
4. [Meta Analysis Flow](#meta-analysis-flow)

---

## Match Reporter Flow

**Persona Example**: **Tim** (casual-competitive, wants quick entry)

1. **Open App**  
   - Tim launches the app on his phone right after an FNM match.
2. **Select Deck**  
   - He chooses his “Mono-Red Aggro” from a dropdown of recent decks.
3. **Enter Opponent**  
   - He types or selects “Azorius Control” for his opponent’s deck.
4. **Input Result**  
   - Tim records “0-2” as the final score.
5. **Add Notes (Optional)**  
   - He can note if he mulled to 6 or who was on the play.
6. **Save & View Stats**  
   - Upon saving, a chart updates, showing Tim’s win rate vs. Control decks.
7. **Decide Next Steps**  
   - Tim sees a prompt for sideboard suggestions vs. control matchups.

---

## Mulligan Tool Flow

**Persona Example**: **Daniel** (returning player, unsure about modern mulligans)

1. **Choose Deck**  
   - Daniel opens the “Mulligan Tool” and selects his decklist (e.g., Pioneer deck).
2. **Generate 7-Card Hand**  
   - The system displays a randomized 7-card hand from Daniel’s 60-card deck.
3. **Compare with 6-Card “Typical”**  
   - The tool side-by-side shows an “average 6-card hand” outline or stats.
4. **Swipe / Decide**  
   - Daniel swipes left if the 7-card is worse than an average 6, or right if better.
5. **Tool Feedback**  
   - The tool may highlight a weak mana base or missing color requirements.
6. **Repeat & Learn**  
   - Daniel tries a few more examples, building confidence in his mulligan choices.

---

## Sideboarding Tool Flow

**Persona Example**: **Lisa** (highly competitive, wants data-driven sideboard)

1. **Select Deck & Opponent**  
   - Lisa chooses her “4-Color Midrange” deck from saved lists, then sets “vs. Tron.”
2. **View Recommendations**  
   - The tool displays recommended ins/outs based on Tron’s weaknesses and Lisa’s deck composition.
3. **Adjust Manually**  
   - Lisa modifies the suggestions (e.g., removing one card she prefers to keep).
4. **Confirm & Save**  
   - She locks in her changes and names the sideboard plan “Tron Matchup v1.”
5. **Export or Print**  
   - Lisa downloads or prints the sideboard guide for tournament prep.

---

## Meta Analysis Flow

**Persona Example**: **Sam** (content creator who wants big-picture insights)

1. **Open Meta Analysis**  
   - Sam navigates to the meta page, selecting “Modern” as the format.
2. **View Deck Popularity**  
   - A pie chart shows the top 5 decks by percentage. Tron is 9%, Murktide is 15%, etc.
3. **Check Win Rates**  
   - A table displays head-to-head matchups (e.g., Tron vs. Murktide: 56% Tron favored).
4. **Winners’ Metagame**  
   - Sam clicks “advanced analysis,” seeing how top decks fare against each other at higher skill/Elo.
5. **System Suggestion**  
   - A summary shows “4C Rhinos has the best chance in a winners’ bracket.”
6. **Share/Stream**  
   - Sam screenshots or references these charts on his live Twitch stream.

---

## Conclusion

These **user flows** illustrate how each persona might interact with the application’s features. They reflect the **storyboards** we created and drive the **requirements** in [requirements.md](./requirements.md).
