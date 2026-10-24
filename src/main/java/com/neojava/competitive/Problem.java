package com.neojava.competitive;

public class Problem {
    private final String id;
    private final String title;
    private final String difficulty;
    private final String topic;
    private final String description;
    private final String constraints;

    public Problem(String id, String title, String difficulty, String topic, String description, String constraints) {
        this.id = id;
        this.title = title;
        this.difficulty = difficulty;
        this.topic = topic;
        this.description = description;
        this.constraints = constraints;
    }

    public String getId() {
        return id;
    }

    public String getTitle() {
        return title;
    }

    public String getDifficulty() {
        return difficulty;
    }

    public String getTopic() {
        return topic;
    }

    public String getDescription() {
        return description;
    }

    public String getConstraints() {
        return constraints;
    }

    @Override
    public String toString() {
        return id + " | " + title + " | " + difficulty + " | " + topic;
    }
}
