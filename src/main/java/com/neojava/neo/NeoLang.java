package com.neojava.neo;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.List;

public class NeoLang {
    private static final NeoChallengeManager challengeManager = new NeoChallengeManager();

    public static void main(String[] args) {
        if (args.length == 0 || "--help".equalsIgnoreCase(args[0]) || "-h".equalsIgnoreCase(args[0])) {
            printUsage();
            return;
        }

        try {
            switch (args[0].toLowerCase()) {
                case "--list" -> challengeManager.listChallenges();
                case "--describe" -> {
                    if (args.length < 2) {
                        throw new IllegalArgumentException("Missing challenge id");
                    }
                    NeoChallenge challenge = challengeManager.findChallenge(args[1]);
                    System.out.println("Challenge: " + challenge.getId());
                    System.out.println("Title: " + challenge.getTitle());
                    System.out.println("Prompt: " + challenge.getPrompt());
                    System.out.println("Starter code:");
                    System.out.println(challenge.getStarterCode());
                }
                case "--run" -> {
                    if (args.length < 2) {
                        throw new IllegalArgumentException("Missing script file");
                    }
                    Path script = Path.of(args[1]);
                    NeoInterpreter interpreter = new NeoInterpreter();
                    List<String> output = interpreter.runFromFile(script);
                    for (String line : output) {
                        System.out.println(line);
                    }
                }
                case "--judge" -> {
                    if (args.length < 3) {
                        throw new IllegalArgumentException("Missing challenge id or script file");
                    }
                    NeoChallenge challenge = challengeManager.findChallenge(args[1]);
                    Path script = Path.of(args[2]);
                    NeoInterpreter interpreter = new NeoInterpreter();
                    List<String> output = interpreter.runFromFile(script);
                    String actual = String.join(System.lineSeparator(), output).trim();
                    String expected = challenge.getExpectedOutput().trim();
                    if (actual.equalsIgnoreCase(expected)) {
                        System.out.println("Challenge passed: " + challenge.getTitle());
                    } else {
                        System.out.println("Challenge failed for " + challenge.getTitle());
                        System.out.println("Expected: " + expected);
                        System.out.println("Actual:   " + actual);
                    }
                }
                default -> {
                    System.out.println("Unknown command: " + args[0]);
                    printUsage();
                }
            }
        } catch (IllegalArgumentException | IOException ex) {
            System.err.println(ex.getMessage());
            printUsage();
        }
    }

    private static void printUsage() {
        System.out.println("NeoLang competitive programming runtime");
        System.out.println("Usage:");
        System.out.println("  java com.neojava.neo.NeoLang --list");
        System.out.println("  java com.neojava.neo.NeoLang --describe neo-sum");
        System.out.println("  java com.neojava.neo.NeoLang --run examples/hello.neo");
        System.out.println("  java com.neojava.neo.NeoLang --judge neo-sum examples/hello.neo");
        System.out.println();
        System.out.println("NeoScript syntax examples:");
        System.out.println("  let x = 10");
        System.out.println("  if x > 5 { print \"large\" } else { print \"small\" }");
        System.out.println("  loop i from 1 to 5 { print i }");
    }
}
