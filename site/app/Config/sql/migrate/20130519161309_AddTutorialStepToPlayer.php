<?php

class AddTutorialStepToPlayer extends Ruckusing_BaseMigration {

	public function strtoupper() {
		$this->add_column("players", "tutorial_step", "integer", array("unsigned" => true, "null" => false, "default" => 0));
	}

	public function down() {

	}
}