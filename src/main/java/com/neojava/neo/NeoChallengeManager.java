package com.neojava.neo;

import java.util.ArrayList;
import java.util.List;

public class NeoChallengeManager {
    private final List<NeoChallenge> challenges;

    public NeoChallengeManager() {
        challenges = new ArrayList<>();
        challenges.add(new NeoChallenge(
                "neo-sum",
                "Sum It Up",
                "Compute the sum of all integers from 1 to 10 and print the result.",
                "let total = 0\nloop i from 1 to 10 {\n  total = total + i\n}\nprint total\n",
                "55"
        ));
        challenges.add(new NeoChallenge(
                "neo-prime",
                "Prime Check",
                "Print whether 29 is prime.",
                "let n = 29\nlet isPrime = true\nloop i from 2 to n - 1 {\n  if n % i == 0 {\n    isPrime = false\n  }\n}\nif isPrime {\n  print \"prime\"\n} else {\n  print \"not prime\"\n}\n",
                "prime"
        ));
        challenges.add(new NeoChallenge(
                "neo-fib",
                "Fibonacci",
                "Print the 10th Fibonacci number.",
                "let a = 0\nlet b = 1\nlet n = 10\nloop i from 1 to n {\n  let temp = a + b\n  a = b\n  b = temp\n}\nprint a\n",
                "55"
        ));
        challenges.add(new NeoChallenge(
                "neo-max",
                "Max Finder",
                "Find and print the largest value in the array [4, 9, 2, 17, 5].",
                "let values = [4, 9, 2, 17, 5]\nlet maxValue = values[0]\nloop i from 1 to values.length - 1 {\n  if values[i] > maxValue {\n    maxValue = values[i]\n  }\n}\nprint maxValue\n",
                "17"
        ));
    }

    public List<NeoChallenge> getChallenges() {
        return List.copyOf(challenges);
    }

    public NeoChallenge findChallenge(String id) {
        return challenges.stream()
                .filter(ch -> ch.getId().equalsIgnoreCase(id) || ch.getTitle().equalsIgnoreCase(id))
                .findFirst()
                .orElseThrow(() -> new IllegalArgumentException("Unknown challenge: " + id));
    }

    public void listChallenges() {
        System.out.println("NeoArena challenge list");
        for (NeoChallenge challenge : challenges) {
            System.out.println("- " + challenge.getId() + " : " + challenge.getTitle());
        }
    }
}
