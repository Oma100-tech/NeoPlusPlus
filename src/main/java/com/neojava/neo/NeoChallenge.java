package com.neojava.neo;

public class NeoChallenge {
    private final String id;
    private final String title;
    private final String prompt;
    private final String starterCode;
    private final String expectedOutput;

    public NeoChallenge(String id, String title, String prompt, String starterCode, String expectedOutput) {
        this.id = id;
        this.title = title;
        this.prompt = prompt;
        this.starterCode = starterCode;
        this.expectedOutput = expectedOutput;
    }

    public String getId() {
        return id;
    }

    public String getTitle() {
        return title;
    }

    public String getPrompt() {
        return prompt;
    }

    public String getStarterCode() {
        return starterCode;
    }

    public String getExpectedOutput() {
        return expectedOutput;
    }

    @Override
    public String toString() {
        return id + " | " + title;
    }
}
