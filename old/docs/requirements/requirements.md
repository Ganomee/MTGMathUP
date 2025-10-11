---
title: "MTG App: User Requirements"
---

# User Requirements

This document outlines the user requirements derived from our **personas** and **storyboards**.

## Table of Contents
- [User Requirements](#user-requirements)
  - [Table of Contents](#table-of-contents)
  - [Match Reporting](#match-reporting)
  - [Mulligan Tool](#mulligan-tool)
  - [Sideboarding Tool](#sideboarding-tool)
  - [Meta Analysis (Bonus)](#meta-analysis-bonus)
  - [Conclusion](#conclusion)

---

## Match Reporting

1. **Quick Match Entry**  
   - The system shall allow users to input match results *quickly* and with minimal friction (especially on mobile).  
   - The system shall allow users to select or input deck names (both theirs and opponent’s) and final match scores.  
   - The system shall support partial data entry (e.g., 1-2 or 2-0, who was on the play, format, etc.) but remain usable with minimal fields if desired.

2. **Aggregated Performance Data**  
   - The system shall display a summary of the user’s match history, including win/loss records and matchup breakdowns.  
   - The system shall allow filtering by deck, date range, or matchup archetype.  
   - The system shall provide some visual representation (charts, graphs) of performance over time.

3. **Data Management & Export**  
   - The system shall store match data securely (behind user authentication).  
   - The system should allow users to export or share data (e.g., CSV, PDF, direct share link).

4. **Personalization**  
   - The system shall allow players to label their decks (e.g., “Tim’s Mono-Red Aggro”) and optionally add side notes or tags.  
   - The system may provide quick deck switching if a user plays multiple decks.

---

## Mulligan Tool

1. **Sample Hand Generation**  
   - The system shall generate sample 7-card hands from a user’s decklist.  
   - The system shall generate corresponding 6-card hands (or 5, 4, etc.) for comparison (the “typical” scenario).

2. **Comparison Interface**  
   - The system shall present a user-friendly way to compare a real/hypothetical 7-card hand vs. an average 6-card hand.  
   - The interface should allow quick “Better or Worse?” judgments (e.g., a swipe or thumbs up/down mechanic).

3. **Data-Driven Guidance**  
   - If feasible, the system should incorporate probability-based insights (e.g., chance of hitting land drops).  
   - The system may display short textual feedback: “Typically, you’d expect X lands after a mull to 6.”

4. **Integration with Decklists**  
   - The system shall import or reference the user’s current decklist for accurate random draws.  
   - The system should allow quick deck switching if the user has multiple decks.

5. **Educational Feedback**  
   - The system should highlight key factors: “Too many high-cost spells,” “Missing color requirements,” etc.  
   - The system may track how often a user chooses to mull or keep, contributing to overall stats.

---

## Sideboarding Tool

1. **Deck Input & Opponent Matchup**  
   - The system shall let users specify their decklist and the opponent’s deck archetype.  
   - The system shall provide a list of suggested cards to bring in or remove for that matchup.

2. **Numerical Power Values**  
   - Each card (mainboard or sideboard) may have an assigned “matchup power value” that indicates how strong it is against a given archetype.  
   - The system can rank or highlight the best sideboard cards for each relevant archetype.

3. **Custom Adjustments**  
   - Users shall be able to override or adjust certain suggestions based on personal experience.  
   - Users can save or export a final “sideboard guide” for each matchup.

4. **Meta & Frequency Data**  
   - If integrated with a meta feature, the system can weigh sideboard card choices by how frequently certain archetypes appear in the meta.  
   - The system should show trade-offs (e.g., “Card A is weaker overall but covers multiple top-tier matchups”).

5. **Sideboard Sharing & Collaboration**  
   - Users may export a sideboard guide to share with teammates or online communities.  
   - The system might support versioning, so users can track changes over time.

---

## Meta Analysis (Bonus)

1. **Meta Deck Data Integration**  
   - The system shall pull or reference current meta deck data from an external source or user-submitted data.  
   - The system shall show the popularity (%) of each deck archetype.

2. **Matchup Win Rates**  
   - The system shall show average win rates between major archetypes.  
   - The system might allow advanced calculations like Elo-based weighting for a “winners’ metagame.”

3. **Deck Recommendation**  
   - The system may suggest the best deck(s) for a given meta or for winning a final bracket (top 8).  
   - The system should list possible “dark horse” choices if the user wants something unexpected.

4. **Customization**  
   - Users can input or adjust the assumed meta distribution (e.g., “20% Aggro, 15% Control”).  
   - The system shall recalculate deck recommendations or sideboard suggestions in real time.

5. **Visualization & Reports**  
   - Provide charts, tier lists, or ranking tables of decks.  
   - Optionally, incorporate a “confidence interval” to show reliability of the data (sample sizes, etc.).

---

## Conclusion

These requirements guide our **feature implementation** to ensure it meets the needs and motivations of our **personas**—from quick mobile logging to advanced meta analysis.  
