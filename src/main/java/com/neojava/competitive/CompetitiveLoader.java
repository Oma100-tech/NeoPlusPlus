package com.neojava.competitive;

import java.util.List;
import java.util.Optional;

public class CompetitiveLoader {
    private final List<Problem> problems;

    public CompetitiveLoader() {
        this.problems = List.of(
                new Problem(
                        "A1",
                        "Two Sum",
                        "Easy",
                        "Arrays",
                        "Return the indices of two numbers in an array that add up to a target value.",
                        "1 <= n <= 10^5, values in the range [-10^9, 10^9]"
                ),
                new Problem(
                        "B2",
                        "Binary Search",
                        "Medium",
                        "Search",
                        "Find a target in a sorted array using the minimum number of comparisons.",
                        "1 <= n <= 10^6, array sorted in ascending order"
                ),
                new Problem(
                        "C3",
                        "Longest Substring Without Repeating Characters",
                        "Hard",
                        "Strings",
                        "Find the length of the longest substring containing no duplicate characters.",
                        "1 <= s.length <= 10^5, lowercase English letters"
                ),
                new Problem(
                        "D4",
                        "Merge Intervals",
                        "Medium",
                        "Greedy",
                        "Merge overlapping intervals and return the merged list sorted by start value.",
                        "1 <= intervals.length <= 10^4, interval bounds fit in 32-bit signed ints"
                )
        );
    }

    public List<Problem> getProblems() {
        return List.copyOf(problems);
    }

    public Optional<Problem> findProblem(String query) {
        String normalized = query.trim().toLowerCase();
        return problems.stream()
                .filter(problem -> problem.getId().equalsIgnoreCase(normalized)
                        || problem.getTitle().toLowerCase().contains(normalized))
                .findFirst();
    }

    public void listProblems() {
        System.out.println("NeoJava Competitive Loader");
        System.out.println("Available problems:");
        for (Problem problem : problems) {
            System.out.printf("- %s: %s [%s] - %s%n",
                    problem.getId(),
                    problem.getTitle(),
                    problem.getDifficulty(),
                    problem.getTopic());
        }
    }

    public void startProblem(String query) {
        Problem problem = findProblem(query)
                .orElseThrow(() -> new IllegalArgumentException("No problem found for query: " + query));

        System.out.println("Loading challenge...");
        System.out.println("Problem ID: " + problem.getId());
        System.out.println("Title: " + problem.getTitle());
        System.out.println("Difficulty: " + problem.getDifficulty());
        System.out.println("Topic: " + problem.getTopic());
        System.out.println("Description: " + problem.getDescription());
        System.out.println("Constraints: " + problem.getConstraints());
        System.out.println("Status: Ready for coding");
    }

    private static void printUsage() {
        System.out.println("Usage:");
        System.out.println("  java com.neojava.competitive.CompetitiveLoader --list");
        System.out.println("  java com.neojava.competitive.CompetitiveLoader --start A1");
        System.out.println("  java com.neojava.competitive.CompetitiveLoader --start \"two sum\"");
    }

    public static void main(String[] args) {
        CompetitiveLoader loader = new CompetitiveLoader();

        if (args.length == 0 || "--help".equalsIgnoreCase(args[0]) || "-h".equalsIgnoreCase(args[0])) {
            printUsage();
            return;
        }

        try {
            switch (args[0].toLowerCase()) {
                case "--list" -> loader.listProblems();
                case "--start" -> {
                    if (args.length < 2) {
                        System.out.println("Missing problem identifier.");
                        printUsage();
                        return;
                    }
                    String query = String.join(" ", java.util.Arrays.copyOfRange(args, 1, args.length));
                    loader.startProblem(query);
                }
                default -> {
                    System.out.println("Unknown command: " + args[0]);
                    printUsage();
                }
            }
        } catch (IllegalArgumentException ex) {
            System.out.println(ex.getMessage());
            System.out.println("Try --list to see available problems.");
        }
    }
}
