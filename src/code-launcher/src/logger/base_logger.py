import logging
import logging.config

import os


def initialize_logger(config_file, log_file_dir, log_file_name):
    os.makedirs(log_file_dir, exist_ok=True)
    logging.config.fileConfig(
        config_file,
        defaults={"logfilename": f"{os.path.join(log_file_dir,log_file_name)}"},
    )
